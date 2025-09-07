using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;

public class MainMenuController : MonoBehaviour
{
    [Header("Debug")]
    private DebugMenu debugMenu = new DebugMenu(false);

    [Header("Dependencies")]
    private ObjectsState globalVars;

    [Header("Navigation Buttons")]
    private Button inventoryButton, equipmentButton, settingsButton;
    private Button selectedButton;

    [Header("View Containers")]
    private VisualElement inventoryView, equipmentView;

    [Header("Inventory UI")]
    private ScrollView itemsCategories;
    private ListView itemsList;
    private VisualElement optionMenu;
    private VisualElement clickCatcher;

    [Header("Data")]
    private List<ItemsCategory> categories = new List<ItemsCategory>();
    private ItemsCategory selectedCategory;
    private InventoryItem selectedItem;

    [Header("Equipment Integration")]
    private EquipmentControl currentEquipmentControl;

    #region Unity Lifecycle
    void Start()
    {
        InitializeDependencies();
        InitializeUI();
        InitializeExampleData();
        PopulateCategories();
        SetInitialView();
    }

    void Update()
    {
        HandleMenuFocus();
    }
    #endregion

    #region Initialization
    private void InitializeDependencies()
    {
        globalVars = FindObjectOfType<ObjectsState>();
        if (globalVars == null)
        {
            Debug.LogError("MainMenuController: ObjectsState not found!");
        }
    }

    private void InitializeUI()
    {
        if (globalVars?.mainMenuUI?.rootVisualElement == null)
        {
            Debug.LogError("MainMenuController: UI root element not found!");
            return;
        }

        var root = globalVars.mainMenuUI.rootVisualElement;

        // Get navigation buttons
        inventoryButton = root.Q<Button>("Inventory");
        equipmentButton = root.Q<Button>("Equipment");
        settingsButton = root.Q<Button>("Settings");

        // Get view containers
        inventoryView = root.Q<VisualElement>("InventoryView");
        equipmentView = root.Q<VisualElement>("EquipmentView");

        // Get inventory UI elements
        itemsCategories = root.Q<ScrollView>("ItemsCategories");
        itemsList = root.Q<ListView>("ItemsList");

        // Register button events
        inventoryButton.clicked += OnInventoryButtonClicked;
        equipmentButton.clicked += OnEquipmentButtonClicked;
        settingsButton.clicked += OnSettingsButtonClicked;
    }

    private void SetInitialView()
    {
        selectedButton = inventoryButton;
        ShowInventoryView();
    }

    private void InitializeExampleData()
    {
        categories.Clear();

        categories.Add(new ItemsCategory
        {
            categoryName = "Weapons",
            categoryType = CategoryType.Weapon,
            items = new List<InventoryItem>
            {
                new InventoryItem("Iron Sword", ItemType.LightWeapon, 1),
                new InventoryItem("Steel Axe", ItemType.HeavyWeapon, 2),
                new InventoryItem("Long Bow", ItemType.Bow, 3),
                new InventoryItem("Crossbow", ItemType.Crossbow, 4)
            }
        });

        categories.Add(new ItemsCategory
        {
            categoryName = "Armor",
            categoryType = CategoryType.Armor,
            items = new List<InventoryItem>
            {
                new InventoryItem("Iron Helmet", ItemType.Helmet, 5),
                new InventoryItem("Steel Chestplate", ItemType.Chestplate, 6),
                new InventoryItem("Chain Leggings", ItemType.Leggings, 7),
                new InventoryItem("Iron Gauntlets", ItemType.Gauntlets, 8)
            }
        });

        categories.Add(new ItemsCategory
        {
            categoryName = "Ammunition",
            categoryType = CategoryType.Ammo,
            items = new List<InventoryItem>
            {
                new InventoryItem("Steel Arrows", ItemType.Arrows, 9),
                new InventoryItem("Iron Bolts", ItemType.Bolts, 10)
            }
        });

        categories.Add(new ItemsCategory
        {
            categoryName = "Magic Items",
            categoryType = CategoryType.MagicItems,
            items = new List<InventoryItem>
            {
                new InventoryItem("Ring of Power", ItemType.MagicItem, 11),
                new InventoryItem("Amulet of Protection", ItemType.MagicItem, 12)
            }
        });

        categories.Add(new ItemsCategory
        {
            categoryName = "Quick Items",
            categoryType = CategoryType.EquipmentItems,
            items = new List<InventoryItem>
            {
                new InventoryItem("Health Potion", ItemType.QuickItem, 13),
                new InventoryItem("Mana Potion", ItemType.QuickItem, 14),
                new InventoryItem("Lockpicks", ItemType.QuickItem, 15)
            }
        });

        categories.Add(new ItemsCategory
        {
            categoryName = "Miscellaneous",
            categoryType = CategoryType.Misc,
            items = new List<InventoryItem>
            {
                new InventoryItem("Gold Coin", ItemType.None, 16),
                new InventoryItem("Ancient Key", ItemType.None, 17)
            }
        });
    }
    #endregion

    #region Navigation
    public void OnInventoryButtonClicked()
    {
        ShowInventoryView();
        currentEquipmentControl = null; // Clear equipment context
        debugMenu.DebugLog("Switched to Inventory view");
    }

    public void OnEquipmentButtonClicked()
    {
        ShowEquipmentView();
        debugMenu.DebugLog("Switched to Equipment view");
    }

    private void OnSettingsButtonClicked()
    {
        ShowSettingsView();
        debugMenu.DebugLog("Switched to Settings view");
    }

    private void ShowInventoryView()
    {
        inventoryView.style.visibility = Visibility.Visible;
        equipmentView.style.visibility = Visibility.Hidden;
    }

    private void ShowEquipmentView()
    {
        inventoryView.style.visibility = Visibility.Hidden;
        equipmentView.style.visibility = Visibility.Visible;
    }

    private void ShowSettingsView()
    {
        inventoryView.style.visibility = Visibility.Hidden;
        equipmentView.style.visibility = Visibility.Hidden;
        // Settings view would be implemented here
    }

    private void HandleMenuFocus()
    {
        if (globalVars != null && globalVars.menuOpen && selectedButton != null)
        {
            selectedButton.Focus();
            selectedButton = null;
        }
    }
    #endregion

    #region Category Management
    private void PopulateCategories()
    {
        if (itemsCategories == null) return;

        itemsCategories.Clear();

        foreach (var category in categories)
        {
            var button = new Button(() => SelectCategory(category))
            {
                text = $"{category.categoryName} ({category.items.Count})"
            };

            itemsCategories.Add(button);
        }
    }

    private void SelectCategory(ItemsCategory category)
    {
        selectedCategory = category;
        debugMenu.DebugLog($"Selected category: {category.categoryName} with {category.items.Count} items");
        PopulateItems(category.items);
    }

    public ItemsCategory GetCategoryByType(CategoryType categoryType)
    {
        return categories.FirstOrDefault(c => c.categoryType == categoryType);
    }
    #endregion

    #region Item List Management
    private void PopulateItems(List<InventoryItem> items, bool isEquipmentMode = false)
    {
        if (itemsList == null) return;

        selectedItem = null;
        itemsList.itemsSource = new List<InventoryItem>(items);
        if (isEquipmentMode && currentEquipmentControl != null)
        {
            var allowedTypes = currentEquipmentControl.GetAllowedItemTypes();
            itemsList.itemsSource = items.Where(item => ((IEnumerable<ItemType>)allowedTypes).Contains(item.itemType)).ToList();
        }

        itemsList.makeItem = () => new Button();

        itemsList.bindItem = (element, index) =>
        {
            if (index >= itemsList.itemsSource.Count) return;

            var item = (InventoryItem)itemsList.itemsSource[index];
            var button = element as Button;

            button.text = item.itemName;

            // Clear previous event handlers
            button.clicked -= null;

            if (isEquipmentMode)
            {
                button.clicked += () => HandleEquipmentModeItemClick(item);
            }
            else
            {
                button.clicked += () => HandleNormalItemClick(item, button);
            }
        };

        itemsList.Rebuild();
    }

    private void HandleNormalItemClick(InventoryItem item, Button button)
    {
        selectedItem = item;
        debugMenu.DebugLog($"Selected item: {item.itemName}");

        var worldPos = button.worldBound.position;
        var menuPos = worldPos + new Vector2(0, button.resolvedStyle.height);

        ShowItemOptionMenu(menuPos, item, button);
    }

    private void HandleEquipmentModeItemClick(InventoryItem item)
    {
        if (currentEquipmentControl != null)
        {
            currentEquipmentControl.EquipItem(item);
            OnEquipmentButtonClicked(); // Return to equipment view
        }
    }
    #endregion

    #region Equipment Integration
    public void QuickEquipment(InventoryItem item)
    {
        switch (item.itemType)
        {
            case ItemType.QuickItem:
                currentEquipmentControl?.EquipToQuickItem(item);
                break;
            case ItemType.LightWeapon:
            case ItemType.HeavyWeapon:
            case ItemType.Bow:
            case ItemType.Crossbow:
                currentEquipmentControl?.EquipToWeapon(item);
                break;
            case ItemType.Helmet:
            case ItemType.Chestplate:
            case ItemType.Leggings:
            case ItemType.Gauntlets:
                currentEquipmentControl?.EquipToArmor(item);
                break;
            case ItemType.Arrows:
            case ItemType.Bolts:
                currentEquipmentControl?.EquipToAmmo(item);
                break;
            case ItemType.MagicItem:
                currentEquipmentControl?.EquipToMagicItem(item);
                break;
            default:
                debugMenu.DebugLog($"No quick equip slot for item type: {item.itemType}");
                break;
        }
        OnEquipmentButtonClicked();
    }
    public void OpenInventoryForEquipment(EquipmentControl equipmentControl, CategoryType categoryType)
    {
        currentEquipmentControl = equipmentControl;
        ShowInventoryView();

        var category = GetCategoryByType(categoryType);
        if (category != null)
        {
            SelectCategory(category);
            PopulateItems(category.items, true); // Equipment mode
        }

        debugMenu.DebugLog($"Opened inventory for equipment: {categoryType}");
    }

    public void RemoveItemFromInventory(InventoryItem item)
    {
        foreach (var category in categories)
        {
            if (category.items.Remove(item))
            {
                debugMenu.DebugLog($"Removed {item.itemName} from inventory");
                RefreshCurrentView();
                return;
            }
        }
    }

    public void ReturnItemToInventory(InventoryItem item)
    {
        var itemCategory = GetItemCategory(item.itemType);
        var category = GetCategoryByType(itemCategory);

        if (category != null)
        {
            category.items.Add(item);
            debugMenu.DebugLog($"Returned {item.itemName} to inventory");
            RefreshCurrentView();
        }
    }

    private CategoryType GetItemCategory(ItemType itemType)
    {
        return itemType switch
        {
            ItemType.LightWeapon or ItemType.HeavyWeapon or ItemType.Bow or ItemType.Crossbow => CategoryType.Weapon,
            ItemType.Helmet or ItemType.Chestplate or ItemType.Leggings or ItemType.Gauntlets => CategoryType.Armor,
            ItemType.Arrows or ItemType.Bolts => CategoryType.Ammo,
            ItemType.MagicItem => CategoryType.MagicItems,
            ItemType.QuickItem => CategoryType.EquipmentItems,
            _ => CategoryType.Misc
        };
    }

    private void RefreshCurrentView()
    {
        PopulateCategories();
        if (selectedCategory != null)
        {
            PopulateItems(selectedCategory.items, currentEquipmentControl != null);
        }
    }
    #endregion

    #region Item Option Menu
    private void ShowItemOptionMenu(Vector2 position, InventoryItem item, VisualElement ownerButton)
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

        // Add menu options
        var equipButton = new Button(() =>
        {
            debugMenu.DebugLog($"Quick equip {item.itemName}");
            currentEquipmentControl = FindAnyObjectByType<EquipmentControl>();
            QuickEquipment(item);
            ClearOptionMenu();
        })
        { text = "Quick Equip" };

        var removeButton = new Button(() =>
        {
            RemoveItemFromInventory(item);
            ClearOptionMenu();
        })
        { text = "Drop" };

        var infoButton = new Button(() =>
        {
            ShowItemInfo(item);
            ClearOptionMenu();
        })
        { text = "Info" };

        optionMenu.Add(equipButton);
        optionMenu.Add(removeButton);
        optionMenu.Add(infoButton);

        root.Add(optionMenu);

        // Handle outside clicks
        clickCatcher.RegisterCallback<ClickEvent>(evt =>
        {
            if (!optionMenu.worldBound.Contains(evt.position) &&
                !ownerButton.worldBound.Contains(evt.position))
            {
                selectedItem = null;
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

    private void ShowItemInfo(InventoryItem item)
    {
        debugMenu.DebugLog($"Item Info - Name: {item.itemName}, Type: {item.itemType}, ID: {item.itemID}");
        // Could open a detailed info panel here
    }
    #endregion

    #region Public API
    public InventoryItem GetSelectedItem()
    {
        return selectedItem;
    }

    public void AddItemToInventory(InventoryItem item)
    {
        var itemCategory = GetItemCategory(item.itemType);
        var category = GetCategoryByType(itemCategory);

        if (category != null)
        {
            category.items.Add(item);
            RefreshCurrentView();
            debugMenu.DebugLog($"Added {item.itemName} to inventory");
        }
    }

    public bool HasItem(int itemID)
    {
        return categories.Any(category => category.items.Any(item => item.itemID == itemID));
    }

    public InventoryItem GetItemByID(int itemID)
    {
        foreach (var category in categories)
        {
            var item = category.items.FirstOrDefault(i => i.itemID == itemID);
            if (item != null) return item;
        }
        return null;
    }
    #endregion

    #region Data Classes
    [System.Serializable]
    public class ItemsCategory
    {
        public string categoryName;
        public CategoryType categoryType;
        public List<InventoryItem> items = new List<InventoryItem>();
    }

    [System.Serializable]
    public class InventoryItem
    {
        public string itemName;
        public ItemType itemType;
        public int itemID;

        public InventoryItem(string name, ItemType type, int id)
        {
            itemName = name;
            itemType = type;
            itemID = id;
        }

        public override string ToString()
        {
            return $"{itemName} ({itemType})";
        }

        public override bool Equals(object obj)
        {
            return obj is InventoryItem other && itemID == other.itemID;
        }

        public override int GetHashCode()
        {
            return itemID.GetHashCode();
        }
    }

    [System.Serializable]
    public class DebugMenu
    {
        public bool toDebug = true;

        public DebugMenu(bool debug = true)
        {
            toDebug = debug;
        }

        public void DebugLog(string message)
        {
            if (toDebug)
            {
                Debug.Log($"[MainMenuController] {message}");
            }
        }
    }
    #endregion

    #region Enums
    public enum CategoryType
    {
        Weapon,
        Armor,
        MagicItems,
        Ammo,
        Misc,
        QuestItems,
        EquipmentItems
    }

    public enum ItemType
    {
        None,
        LightWeapon,
        HeavyWeapon,
        Bow,
        Crossbow,
        Helmet,
        Chestplate,
        Leggings,
        Gauntlets,
        Arrows,
        Bolts,
        MagicItem,
        QuickItem,
        Potion
    }
    #endregion
}