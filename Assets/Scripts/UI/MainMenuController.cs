using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;

[RequireComponent(typeof(ObjectsState))]
public class MainMenuController : MonoBehaviour
{
    private DebugMenu debugMenu = new DebugMenu(false);

    public Button inventoryButton;
    public Button settingsButton;
    private Button selectedButton;
    private ObjectsState GlobalVars;

    // InventoryView
    private VisualElement inventoryView;
    private ScrollView itemsCategories;
    private ListView itemsList;
    private List<ItemsCategories> categories = new List<ItemsCategories>();
    private InventoryItem selectedItem;

    class ItemsCategories
    {
        public string categoryName;
        public List<InventoryItem> items;
    }
    private ItemsCategories selectedCategory;

    void Start()
    {
        GlobalVars = FindObjectOfType<ObjectsState>();
        var root = GlobalVars.mainMenuUI.rootVisualElement;

        inventoryButton = root.Q<Button>("Inventory");
        settingsButton = root.Q<Button>("Settings");

        inventoryView = root.Q<VisualElement>("InventoryView");
        itemsCategories = root.Q<ScrollView>("ItemsCategories");
        itemsList = root.Q<ListView>("ItemsList");

        selectedButton = inventoryButton;
        inventoryButton.clicked += OnInventoryButtonClicked;
        settingsButton.clicked += OnSettingsButtonClicked;

        // ---- Example data ----
        categories.Add(new ItemsCategories
        {
            categoryName = "Weapons",
            items = new List<InventoryItem> {
            new InventoryItem("Sword", 1),
            new InventoryItem("Axe", 2),
            new InventoryItem("Bow", 3)
        }
        });

        categories.Add(new ItemsCategories
        {
            categoryName = "Potions",
            items = new List<InventoryItem> {
            new InventoryItem("Health Potion", 4),
            new InventoryItem("Mana Potion", 5)
        }
        });

        categories.Add(new ItemsCategories
        {
            categoryName = "Armor",
            items = new List<InventoryItem> {
            new InventoryItem("Helmet", 6),
            new InventoryItem("Chestplate", 7)
        }
        });

        PopulateCategories();
    }

    private void PopulateCategories()
    {
        itemsCategories.Clear(); // clear old entries

        foreach (var category in categories)
        {
            var button = new Button();
            button.text = category.categoryName;

            button.clicked += () =>
            {
                selectedCategory = category;
                debugMenu.DebugLog($"Selected category: {category.categoryName} with {category.items.Count} items");
                PopulateItems(selectedCategory.items);
            };

            itemsCategories.Add(button);
        }
    }

    private void PopulateItems(List<InventoryItem> items)
    {
        selectedItem = null;

        // Reassign a new list instance to avoid ListView caching issues
        itemsList.itemsSource = new List<InventoryItem>(items);

        itemsList.makeItem = () =>
        {
            return new Button();
        };

        itemsList.bindItem = (element, index) =>
        {
            var item = (InventoryItem)itemsList.itemsSource[index];
            var btn = element as Button;

            // Clear previous listeners
            btn.clicked -= null;

            btn.text = item.itemName;

            // Always bind by item reference, not index
            btn.clicked += () =>
            {
                selectedItem = item;
                debugMenu.DebugLog($"Selected item: {item.itemName}");
            };
        };

        itemsList.Rebuild();
    }

    void Update()
    {
        if (GlobalVars.menuOpen && selectedButton != null)
        {
            selectedButton.Focus();
            selectedButton = null;
        }

        if (selectedItem != null && Input.GetKeyDown(KeyCode.R))
        {
            if (selectedCategory != null && selectedCategory.items.Contains(selectedItem))
            {
                selectedCategory.items.Remove(selectedItem);
                debugMenu.DebugLog($"Item deleted: {categories.IndexOf(selectedCategory)} - {selectedItem.itemName}");
                debugMenu.DebugLog($"Remaining items in category '{selectedCategory.categoryName}': {selectedCategory.items.Count}");
                debugMenu.DebugLog("All items:" + string.Join(", ", categories.SelectMany(c => c.items).Select(i => i.itemName)));

                // Refresh the list
                PopulateItems(selectedCategory.items);
            }
        }
    }

    private void OnInventoryButtonClicked()
    {
        inventoryView.style.visibility = Visibility.Visible;
        debugMenu.DebugLog("Inventory button clicked");
    }

    private void OnSettingsButtonClicked()
    {
        inventoryView.style.visibility = Visibility.Hidden;
        debugMenu.DebugLog("Settings button clicked");
    }

    public class InventoryItem
    {
        public string itemName;
        public int itemID;

        public InventoryItem(string name, int id)
        {
            itemName = name;
            itemID = id;
        }
        public void AddItemToInventory(InventoryItem item, List<InventoryItem> inventory)
        {
            inventory.Add(item);
        }
        public void RemoveItemFromInventory(InventoryItem item, List<InventoryItem> inventory)
        {
            inventory.Remove(item);
        }
    }

    [System.Serializable]
    public class DebugMenu
    {
        public bool to_Debug = true;
        public DebugMenu(bool debug = true)
        {
            to_Debug = debug;
        }

        public void DebugLog(string message)
        {
            if (to_Debug)
            {
                Debug.Log(message);
            }
        }
    }
}
