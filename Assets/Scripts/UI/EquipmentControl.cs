using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class EquipmentControl : MonoBehaviour
{
    [Header("Dependencies")]
    private ObjectsState globalVars;
    private MainMenuController mainMenuController;

    [Header("UI Elements")]
    private VisualElement optionMenu;
    private VisualElement clickCatcher;

    [Header("Equipment State")]
    public EquipmentSlot selectedSlot;
    public Dictionary<string, EquipmentSlot> equipmentSlots;

    #region Unity Lifecycle
    void Start()
    {
        InitializeDependencies();
        InitializeEquipmentSlots();
        RegisterSlotEvents();
    }
    #endregion

    #region Initialization
    private void InitializeDependencies()
    {
        globalVars = FindObjectOfType<ObjectsState>();
        mainMenuController = FindObjectOfType<MainMenuController>();

        if (globalVars == null || mainMenuController == null)
        {
            Debug.LogError("EquipmentControl: Missing required dependencies!");
        }
    }

    private void InitializeEquipmentSlots()
    {
        if (globalVars?.mainMenuUI?.rootVisualElement == null)
        {
            Debug.LogError("EquipmentControl: UI root element not found!");
            return;
        }

        var root = globalVars.mainMenuUI.rootVisualElement;
        equipmentSlots = new Dictionary<string, EquipmentSlot>();

        // Define slot configurations
        var slotConfigs = new[]
        {
            // Weapons
            ("LW1", MainMenuController.CategoryType.Weapon),
            ("LW2", MainMenuController.CategoryType.Weapon),
            ("LW3", MainMenuController.CategoryType.Weapon),
            ("RW1", MainMenuController.CategoryType.Weapon),
            ("RW2", MainMenuController.CategoryType.Weapon),
            ("RW3", MainMenuController.CategoryType.Weapon),
            
            // Armor
            ("Helmet", MainMenuController.CategoryType.Armor),
            ("Chestplate", MainMenuController.CategoryType.Armor),
            ("Leggings", MainMenuController.CategoryType.Armor),
            ("Gauntlets", MainMenuController.CategoryType.Armor),
            
            // Ammunition
            ("Arrows1", MainMenuController.CategoryType.Ammo),
            ("Arrows2", MainMenuController.CategoryType.Ammo),
            ("Bolts1", MainMenuController.CategoryType.Ammo),
            ("Bolts2", MainMenuController.CategoryType.Ammo),
            
            // Magic Items
            ("MagicItem1", MainMenuController.CategoryType.MagicItems),
            ("MagicItem2", MainMenuController.CategoryType.MagicItems),
            ("MagicItem3", MainMenuController.CategoryType.MagicItems),
            ("MagicItem4", MainMenuController.CategoryType.MagicItems),
            
            // Quick Items
            ("QuickItem1", MainMenuController.CategoryType.EquipmentItems),
            ("QuickItem2", MainMenuController.CategoryType.EquipmentItems),
            ("QuickItem3", MainMenuController.CategoryType.EquipmentItems),
            ("QuickItem4", MainMenuController.CategoryType.EquipmentItems)
        };

        // Create equipment slots
        foreach (var (slotName, category) in slotConfigs)
        {
            var button = root.Q<Button>(slotName);
            if (button != null)
            {
                var slot = new EquipmentSlot(button, category, null);
                equipmentSlots[slotName] = slot;
            }
            else
            {
                Debug.LogWarning($"EquipmentControl: Button '{slotName}' not found in UI!");
            }
        }
    }

    private void RegisterSlotEvents()
    {
        foreach (var slot in equipmentSlots.Values)
        {
            if (slot.button != null)
            {
                slot.button.clicked += () => OnSlotClicked(slot);
            }
        }
    }
    #endregion

    #region Event Handlers
    private void OnSlotClicked(EquipmentSlot slot)
    {
        selectedSlot = slot;

        if (slot.HasItem())
        {
            // Show equipped item info or options
            ShowEquippedItemOptions(slot);
        }
        else
        {
            // Show equip options
            ShowEquipOptions(slot);
        }
    }

    private void ShowEquippedItemOptions(EquipmentSlot slot)
    {
        var position = slot.button.worldBound.position;
        ShowOptionMenu(position, new (string, System.Action)[]
        {
            ("Unequip", () => UnequipItem(slot)),
            ("Replace", () => ReplaceItem(slot)),
            ("Info", () => ShowItemInfo(slot.item))
        });
    }

    private void ShowEquipOptions(EquipmentSlot slot)
    {
        var position = slot.button.worldBound.position;
        ShowOptionMenu(position, new (string, System.Action)[]
        {
            ("Equip", () => OpenInventoryForEquip(slot))
        });
    }
    #endregion

    #region Equipment Actions
    public void EquipItem(InventoryItem item)
    {
        if (selectedSlot == null)
        {
            Debug.LogWarning("EquipmentControl: No slot selected for equipping!");
            return;
        }

        if (!CanEquipToSlot(item, selectedSlot))
        {
            Debug.LogWarning($"EquipmentControl: Cannot equip {item.itemName} to {selectedSlot.button.name}!");
            return;
        }

        // Store previous item for potential return to inventory
        var previousItem = selectedSlot.item;

        // Equip new item
        selectedSlot.SetItem(item);
        UpdateSlotDisplay(selectedSlot);

        // Return previous item to inventory if existed
        if (previousItem != null)
        {
            mainMenuController.ReturnItemToInventory(previousItem);
        }

        // Remove new item from inventory
        mainMenuController.RemoveItemFromInventory(item);

        Debug.Log($"Equipped {item.itemName} to {selectedSlot.button.name}");
        selectedSlot = null;
    }

    private void UnequipItem(EquipmentSlot slot)
    {
        if (!slot.HasItem()) return;

        var item = slot.item;
        slot.SetItem(null);
        UpdateSlotDisplay(slot);
        Player_controller player = FindObjectOfType<Player_controller>();
        if (player != null)
        {
            player.UndrawWeapon(item, item); // Pass the item to undraw
        }

        // Return item to inventory
        mainMenuController.ReturnItemToInventory(item);

        Debug.Log($"Unequipped {item.itemName} from {slot.button.name}");
        ClearOptionMenu();
    }

    private void ReplaceItem(EquipmentSlot slot)
    {
        selectedSlot = slot;
        OpenInventoryForEquip(slot);
    }

    private void OpenInventoryForEquip(EquipmentSlot slot)
    {
        selectedSlot = slot;
        mainMenuController.OpenInventoryForEquipment(this, slot.category);
        ClearOptionMenu();
    }

    private bool CanEquipToSlot(InventoryItem item, EquipmentSlot slot)
    {
        // Check if item category matches slot category
        var itemCategory = GetItemCategory(item.itemSubType);
        return itemCategory == slot.category;
    }

    private MainMenuController.CategoryType GetItemCategory(MainMenuController.ItemType itemType)
    {
        return itemType switch
        {
            MainMenuController.ItemType.LightWeapon => MainMenuController.CategoryType.Weapon,
            MainMenuController.ItemType.HeavyWeapon => MainMenuController.CategoryType.Weapon,
            MainMenuController.ItemType.Bow => MainMenuController.CategoryType.Weapon,
            MainMenuController.ItemType.Crossbow => MainMenuController.CategoryType.Weapon,
            MainMenuController.ItemType.Helmet => MainMenuController.CategoryType.Armor,
            MainMenuController.ItemType.Chestplate => MainMenuController.CategoryType.Armor,
            MainMenuController.ItemType.Leggings => MainMenuController.CategoryType.Armor,
            MainMenuController.ItemType.Gauntlets => MainMenuController.CategoryType.Armor,
            MainMenuController.ItemType.Arrows => MainMenuController.CategoryType.Ammo,
            MainMenuController.ItemType.Bolts => MainMenuController.CategoryType.Ammo,
            MainMenuController.ItemType.MagicItem => MainMenuController.CategoryType.MagicItems,
            MainMenuController.ItemType.QuickItem => MainMenuController.CategoryType.EquipmentItems,
            _ => MainMenuController.CategoryType.Misc
        };
    }

    private void UpdateSlotDisplay(EquipmentSlot slot)
    {
        if (slot.HasItem())
        {
            slot.button.text = slot.item.itemName;
            slot.button.style.color = new StyleColor(Color.white);

            // Could add item icon here
        }
        else
        {
            slot.button.text = GetEmptySlotText(slot.button.name);
            slot.button.style.color = Color.white;
            slot.button.style.backgroundImage = null;
            // set defult empty slot image
        }
    }

    private string GetEmptySlotText(string slotName)
    {
        return slotName switch
        {
            "LW1" or "LW2" or "LW3" => "Left Weapon",
            "RW1" or "RW2" or "RW3" => "Right Weapon",
            "Helmet" => "Helmet",
            "Chestplate" => "Chestplate",
            "Leggings" => "Leggings",
            "Gauntlets" => "Gauntlets",
            "Arrows1" or "Arrows2" => "Arrows",
            "Bolts1" or "Bolts2" => "Bolts",
            var name when name.StartsWith("MagicItem") => "Magic Item",
            var name when name.StartsWith("QuickItem") => "Quick Item",
            _ => "Empty"
        };
    }

    private void ShowItemInfo(InventoryItem item)
    {
        if (item != null)
        {
            Debug.Log($"Item Info: {item.itemName} (Type: {item.itemSubType}, ID: {item.itemID})");
            // Could open a detailed info panel here
        }
        ClearOptionMenu();
    }
    #endregion

    #region Option Menu
    private void ShowOptionMenu(Vector2 position, (string text, System.Action action)[] options)
    {
        ClearOptionMenu();

        if (globalVars?.mainMenuUI?.rootVisualElement == null) return;

        var root = globalVars.mainMenuUI.rootVisualElement;

        // Create click catcher
        clickCatcher = new VisualElement
        {
            style = {
                position = Position.Absolute,
                left = 0, top = 0, right = 0, bottom = 0,
                backgroundColor = new Color(0, 0, 0, 0)
            }
        };
        root.Add(clickCatcher);

        // Create option menu
        optionMenu = new VisualElement
        {
            style = {
                position = Position.Absolute,
                left = position.x,
                top = position.y,
                backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.9f),
                paddingLeft = 8, paddingRight = 8,
                paddingTop = 6, paddingBottom = 6,
                borderTopLeftRadius = 4, borderTopRightRadius = 4,
                borderBottomLeftRadius = 4, borderBottomRightRadius = 4,
                borderLeftWidth = 1, borderRightWidth = 1,
                borderTopWidth = 1, borderBottomWidth = 1,
                borderLeftColor = Color.gray, borderRightColor = Color.gray,
                borderTopColor = Color.gray, borderBottomColor = Color.gray
            }
        };

        // Add option buttons
        foreach (var (text, action) in options)
        {
            var button = new Button(action) { text = text };
            button.style.marginBottom = 2;
            optionMenu.Add(button);
        }

        root.Add(optionMenu);

        // Handle outside clicks
        clickCatcher.RegisterCallback<ClickEvent>(evt =>
        {
            if (!optionMenu.worldBound.Contains(evt.position))
            {
                ClearOptionMenu();
            }
        });
    }

    private void ClearOptionMenu()
    {
        optionMenu?.RemoveFromHierarchy();
        optionMenu = null;
        clickCatcher?.RemoveFromHierarchy();
        clickCatcher = null;
    }
    #endregion

    #region Public API
    public EquipmentSlot GetSlot(string slotName)
    {
        return equipmentSlots.TryGetValue(slotName, out var slot) ? slot : null;
    }

    public List<InventoryItem> GetAllEquippedItems()
    {
        var equippedItems = new List<InventoryItem>();
        foreach (var slot in equipmentSlots.Values)
        {
            if (slot.HasItem())
            {
                equippedItems.Add(slot.item);
            }
        }
        return equippedItems;
    }

    public string HasItemEquipped(int itemID)
    {
        foreach (var (slotKey, slotValue) in equipmentSlots)
        {
            Debug.Log($"Checking slot {slotKey} for item ID {itemID}");
            if (slotValue.HasItem() && slotValue.item.itemID == itemID)
            {
                return (string)slotKey;
            }
        }
        return null;
    }

    internal object GetAllowedItemTypes()
    {
        if (selectedSlot == null) return null;

        switch (selectedSlot.button.name)
        {
            case "LW1":
            case "LW2":
            case "LW3":
            case "RW1":
            case "RW2":
            case "RW3":
                return new[] { MainMenuController.ItemType.LightWeapon, MainMenuController.ItemType.HeavyWeapon, MainMenuController.ItemType.Bow, MainMenuController.ItemType.Crossbow };
            case "Helmet":
                return new[] { MainMenuController.ItemType.Helmet };
            case "Chestplate":
                return new[] { MainMenuController.ItemType.Chestplate };
            case "Leggings":
                return new[] { MainMenuController.ItemType.Leggings };
            case "Gauntlets":
                return new[] { MainMenuController.ItemType.Gauntlets };
            case "Arrows1":
            case "Arrows2":
                return new[] { MainMenuController.ItemType.Arrows };
            case "Bolts1":
            case "Bolts2":
                return new[] { MainMenuController.ItemType.Bolts };
            case var name when name.StartsWith("MagicItem"):
                return new[] { MainMenuController.ItemType.MagicItem };
            case var name when name.StartsWith("QuickItem"):
                return new[] { MainMenuController.ItemType.QuickItem };
            default:
                return null;
        }
    }

    internal void EquipToQuickItem(InventoryItem item)
    {
        selectedSlot = FindEmptySlot(MainMenuController.CategoryType.EquipmentItems);
        EquipItem(item);
    }

    internal void EquipToWeapon(InventoryItem item)
    {
        selectedSlot = FindEmptySlot(MainMenuController.CategoryType.Weapon);
        EquipItem(item);
    }

    internal void EquipToArmor(InventoryItem item)
    {
        selectedSlot = FindEmptySlot(MainMenuController.CategoryType.Armor, item.itemSubType);
        EquipItem(item);
    }

    internal void EquipToAmmo(InventoryItem item)
    {
        selectedSlot = FindEmptySlot(MainMenuController.CategoryType.Ammo, item.itemSubType);
        EquipItem(item);
    }

    internal void EquipToMagicItem(InventoryItem item)
    {
        selectedSlot = FindEmptySlot(MainMenuController.CategoryType.MagicItems);
        EquipItem(item);
    }
    #endregion

    #region Utils
    private EquipmentSlot FindEmptySlot(MainMenuController.CategoryType categoryType,
    MainMenuController.ItemType itemType = MainMenuController.ItemType.None)
    {
        foreach (var slot in equipmentSlots.Values)
        {

            if (itemType == MainMenuController.ItemType.None &&
            slot.category == categoryType &&
            slot.IsEmpty())
            {
                return slot;
            }
            else
            {
                switch (itemType)
                {
                    case MainMenuController.ItemType.Helmet when slot.button.name == "Helmet":
                    case MainMenuController.ItemType.Chestplate when slot.button.name == "Chestplate":
                    case MainMenuController.ItemType.Leggings when slot.button.name == "Leggings":
                    case MainMenuController.ItemType.Gauntlets when slot.button.name == "Gauntlets":
                    case MainMenuController.ItemType.Arrows when (slot.button.name == "Arrows1" || slot.button.name == "Arrows2"):
                    case MainMenuController.ItemType.Bolts when (slot.button.name == "Bolts1" || slot.button.name == "Bolts2"):
                        return slot;
                }
            }

        }
        // If no empty slot found, return the first slot in this category
        foreach (var slot in equipmentSlots.Values)
        {
            if (slot.category == categoryType)
            {
                return slot;
            }
        }
        return null;
    }
    #endregion

    #region Equipment Slot Class
    public class EquipmentSlot
    {
        public Button button;
        public MainMenuController.CategoryType category;
        public InventoryItem item;

        public EquipmentSlot(Button button, MainMenuController.CategoryType category, InventoryItem item)
        {
            this.button = button;
            this.category = category;
            this.item = item;
        }

        public void SetItem(InventoryItem newItem)
        {
            item = newItem;
        }

        public bool HasItem()
        {
            return item != null;
        }

        public bool IsEmpty()
        {
            return item == null;
        }
    }
    #endregion
}