using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using UnityEngine;
using System.Data.Common;

public class SavePlayerData : MonoBehaviour
{
    public Player_controller playerController;
    public MainMenuController mainMenuController;
    [SerializeField] private float autoSaveInterval = 60 * 5f; // Auto-save every 5 minutes default

    private void Start()
    {
        // Start automatic saving
        InvokeRepeating(nameof(AutoSave), autoSaveInterval, autoSaveInterval);
        ItemsManager.Initialize(() =>
            mainMenuController?.InitializeSavedItemsInInventory(ItemsManager.ConvertDataArrayToInventoryItemsArray(SaveSystem.Load().items))
        );
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

        SaveSystem.Save(new PlayerSaveData.Data(
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
        public int[] position; // x, y, z
        public Dictionary<int, ItemData> items; // IDs of items in inventory

        public PlayerSaveData(Vector3 position, List<MainMenuController.ItemsCategory> inventory, MainMenuController mainMenuController = null)
        {
            this.position = new int[] { (int)position.x, (int)position.y, (int)position.z };
            this.items = new Dictionary<int, ItemData>();
            this.mainMenuController = mainMenuController;

            foreach (var category in inventory)
            {
                foreach (var item in category.items)
                {
                    string equipSlot = "";
                    if (mainMenuController?.GetEquipmentControl() != null)
                        equipSlot = mainMenuController.GetEquipmentControl().HasItemEquipped(item.itemID) ?? "";

                    items[item.itemID] = new ItemData(item.itemName,
                    item.itemID,
                    equipSlot);
                }
            }
        }

        [System.Serializable]
        public class Data
        {
            public int[] position; // x, y, z
            public Dictionary<int, ItemData> items; // IDs of items in inventory

            public Data(int[] position, Dictionary<int, ItemData> items)
            {
                this.position = position;
                this.items = items;
            }
        }
    }

    [System.Serializable]
    public static class SaveSystem
    {
        private static string savePath = Application.persistentDataPath + "/player_state.bin";

        internal static void Save(PlayerSaveData.Data data)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            using (FileStream stream = new FileStream(savePath, FileMode.Create))
            {
                formatter.Serialize(stream, data);
            }
            Debug.Log($"Save created at {savePath}");
        }

        internal static PlayerSaveData.Data Load()
        {
            if (!File.Exists(savePath))
            {
                Debug.LogWarning("Save file not found!");
                return null;
            }

            try
            {
                BinaryFormatter formatter = new BinaryFormatter();
                using (FileStream stream = new FileStream(savePath, FileMode.Open))
                {
                    var data = (PlayerSaveData.Data)formatter.Deserialize(stream);
                    Debug.Log($"Save loaded from {savePath}");
                    Debug.Log($"Loaded {data.items.Count} items"); // Check items count
                    return data;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to load save: {ex}");
                return null;
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
