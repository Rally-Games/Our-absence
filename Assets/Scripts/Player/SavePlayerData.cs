using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions.Must;


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
            mainMenuController?.InitializeSavedItemsInInventory(ItemsManager.ConvertDataArrayToInventoryItemsArray(data.items));
            EquipmentControl equipmentControl = mainMenuController?.GetEquipmentControl();
            if (equipmentControl != null)
            {
                foreach (var item in data.items)
                {
                    if (!string.IsNullOrEmpty(item.Value.equipSlot))
                    {
                        InventoryItem inventoryItem = ItemsManager.GetItemByID(item.Value.itemID);
                        if (inventoryItem != null)
                        {
                            equipmentControl.EquipItem(inventoryItem, equipmentControl.GetSlot(item.Value.equipSlot));
                        }
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
        public Dictionary<int, ItemData> items; // IDs of items in inventory

        public PlayerSaveData(Vector3 position, List<MainMenuController.ItemsCategory> inventory, MainMenuController mainMenuController = null)
        {
            this.position = new float[] { (float)position.x, (float)position.y, (float)position.z };
            this.items = new Dictionary<int, ItemData>();
            this.mainMenuController = mainMenuController;

            foreach (var category in inventory)
            {
                int i = 0;
                foreach (var item in category.items)
                {
                    string equipSlot = "";
                    if (mainMenuController?.GetEquipmentControl() != null)
                        equipSlot = mainMenuController.GetEquipmentControl().HasItemEquipped(item.ItemID) ?? "";

                    items[i++] = new ItemData(item.ItemName,
                    item.ItemID,
                    equipSlot);
                }
            }
        }

        [System.Serializable]
        public class Data
        {
            public float[] position; // x, y, z
            public Dictionary<int, ItemData> items; // IDs of items in inventory

            public Data(float[] position, Dictionary<int, ItemData> items)
            {
                this.position = position;
                this.items = items;
            }
        }
    }



    [System.Serializable]
    public class ItemData
    {
        public string itemName;
        public int itemID;
        public string equipSlot;

        public ItemData(string itemName, int itemID, string equipSlot = null)
        {
            this.itemName = itemName;
            this.itemID = itemID;
            this.equipSlot = equipSlot;
        }
    }
}
