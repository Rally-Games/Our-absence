using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions.Must;
using Newtonsoft.Json;


public class SavePlayerData : MonoBehaviour
{
    public Player_controller playerController;
    public MainMenuController mainMenuController;
    [SerializeField] private float autoSaveInterval = 60 * 5f; // Auto-save every 5 minutes default

    private void Start()
    {
        // Start automatic saving
        InvokeRepeating(nameof(AutoSave), autoSaveInterval, autoSaveInterval);
        PlayerSaveData.Data data = SaveAndLoad.LoadPlayerState();
        ItemsManager.Initialize(() =>
        {
            mainMenuController?.InitializeSavedItemsInInventory(ItemsManager.ConvertDataDicToInventoryItemsArrayOptimized(data.items));
            EquipmentControl equipmentControl = mainMenuController?.GetEquipmentControl();
            if (equipmentControl != null)
            {
                foreach (var item in data.items)
                {
                    if (!string.IsNullOrEmpty(item.Value.equippedSlotName))
                    {
                        equipmentControl.EquipItem(item.Value, equipmentControl.GetSlot(item.Value.equippedSlotName));
                    }
                }
            }
        }
        );
        if (data?.position != null && data.position.Length == 3)
            playerController.transform.position = new Vector3(data.position[0], data.position[1], data.position[2]);
    }

    private void Update()
    {
        // Keep manual save with F5 for debugging
        if (Input.GetKeyDown(KeyCode.F5))
        {
            Save();
        }
    }

    private void AutoSave()
    {
        Save();
        Debug.Log("Autosave complete!");
    }

    private void Save()
    {
        if (playerController == null || mainMenuController == null)
        {
            Debug.LogWarning("Cannot save: missing references.");
            return;
        }

        PlayerSaveData playerData = new PlayerSaveData(
            playerController.transform.position,
            mainMenuController.GetAllInventory(),
            mainMenuController
        );

        SaveAndLoad.SavePlayerState(new PlayerSaveData.Data(
            playerData.position,
            playerData.items
        ));
    }

    [System.Serializable]
    public class PlayerSaveData
    {
        //public int health;
        //public int stamina;
        private MainMenuController mainMenuController;
        public float[] position; // x, y, z
        public Dictionary<int, ItemDataInstance> items; // IDs of items in inventory

        public PlayerSaveData(Vector3 position, List<MainMenuController.ItemsCategory> inventory, MainMenuController mainMenuController = null)
        {
            this.position = new float[] { (float)position.x, (float)position.y, (float)position.z };
            this.items = new Dictionary<int, ItemDataInstance>();
            this.mainMenuController = mainMenuController;

            foreach (var category in inventory)
            {
                int i = 0;
                foreach (var item in category.items)
                {
                    string equipSlot = "";
                    if (mainMenuController?.GetEquipmentControl() != null)
                        equipSlot = mainMenuController.GetEquipmentControl().HasItemEquipped(item._definition.ItemID) ?? "";

                    items[i++] = new ItemDataInstance(
                        item._definition
                    );
                }
            }
        }

        [System.Serializable]
        public class Data
        {
            public float[] position; // x, y, z
            [JsonIgnore] public Dictionary<int, ItemDataInstance> items;
            public Dictionary<int, ItemData> ItemsDataToSave;

            public Data(float[] position, Dictionary<int, ItemDataInstance> items)
            {
                this.position = position;
                this.items = items;
                foreach (var item in items)
                {
                    this.ItemsDataToSave[item.Key] = new ItemData(item.Value);
                }
            }
        }

        [System.Serializable]
        public class ItemData
        {
            public int itemID;
            public int quantity;
            public float currentDurability;
            public string equippedSlotName; // Name of the slot where the item is equipped, if any
            public bool isFavorite;
            public int enchantmentLevel;
            public float damageModifier;

            public ItemData(ItemDataInstance item)
            {
                this.itemID = item.itemID;
                this.quantity = item.quantity;
                this.currentDurability = item.currentDurability;
                this.equippedSlotName = item.equippedSlotName;
                this.isFavorite = item.isFavorite;
                this.enchantmentLevel = item.enchantmentLevel;
                this.damageModifier = item.damageModifier;
            }
        }
    }
}
