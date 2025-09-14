using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public static class ItemsManager
{

    public static List<InventoryItem> allItems = new List<InventoryItem>();
    public static Dictionary<int, InventoryItem> itemsById = new Dictionary<int, InventoryItem>();
    private static bool isLoaded = false;

    public static void Initialize(System.Action onComplete = null)
    {
        if (isLoaded)
        {
            onComplete?.Invoke();
            return;
        }

        allItems.Clear();
        itemsById.Clear();

        Addressables.LoadAssetsAsync<InventoryItem>("InventoryItems", item =>
        {
            allItems.Add(item);
        }).Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                foreach (var item in allItems)
                {
                    if (!itemsById.ContainsKey(item.itemID))
                        itemsById.Add(item.itemID, item);
                    else
                        Debug.LogWarning($"Duplicate ID detected: {item.itemID} ({item.name})");
                }

                isLoaded = true;
                Debug.Log($"All items loaded! Count: {allItems.Count}");
                onComplete?.Invoke();
            }
            else
            {
                Debug.LogError("Failed to load items via Addressables!");
            }
        };
    }

    public static List<InventoryItem> FilterItemsByType(MainMenuController.CategoryType type, Dictionary<int, SavePlayerData.ItemData> items = null)
    {
        List<InventoryItem> filteredItems = new List<InventoryItem>();
        foreach (var (myItemKey, myItemValue) in items)
        {
            foreach (var item in allItems)
            {
                Debug.Log($"Comparing item ID {myItemValue.itemID} with {item.itemID} of type {item.itemCategory}");
                if (item.itemCategory == type && myItemValue.itemID == item.itemID)
                {
                    filteredItems.Append(item);
                }
            }
        }
        return filteredItems;
    }
    public static List<InventoryItem> ConvertDataArrayToInventoryItemsArray(Dictionary<int, SavePlayerData.ItemData> items = null)
    {
        List<InventoryItem> filteredItems = new List<InventoryItem>();
        foreach (var myItem in items)
        {
            foreach (var item in allItems)
            {
                if (myItem.Value.itemID == item.itemID)
                {
                    filteredItems.Add(item);
                }
            }
        }
        return filteredItems;
    }

    internal static InventoryItem GetItemByID(int ID)
    {
        foreach (var item in allItems)
        {
            if (item.itemID == ID)
            {
                return item;
            }
        }
        return null;
    }

    [MenuItem("Tools/Items/Check for Duplicate IDs")]
    public static void CheckForDuplicateIDs()
    {
        if (allItems == null || allItems.Count == 0)
        {
            Debug.LogWarning("No items found in Resources!");
            return;
        }

        Dictionary<int, List<InventoryItem>> idMap = new Dictionary<int, List<InventoryItem>>();
        foreach (var item in allItems)
        {
            if (item == null) continue;

            if (!idMap.ContainsKey(item.itemID))
                idMap[item.itemID] = new List<InventoryItem>();

            idMap[item.itemID].Add(item);
        }

        // Check duplicates
        bool hasDuplicates = false;
        foreach (var kvp in idMap)
        {
            if (kvp.Value.Count > 1)
            {
                hasDuplicates = true;
                Debug.LogError($"Duplicate ID {kvp.Key} found in:");
                foreach (var item in kvp.Value)
                {
                    Debug.LogError($"   {item.name}", item);
                }
            }
        }

        if (!hasDuplicates)
            Debug.Log("✅ No duplicate Item IDs found!");
    }
}
