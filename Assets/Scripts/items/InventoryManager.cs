using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager instance;
    public List<item> items = new List<item>();

    public Transform itemsContent;
    public GameObject inventoryItem;

    void Awake()
    {
        instance = this;
    }

    public void Add(item newItem)
    {
        items.Add(newItem);
    }

    public void Remove(item newItem)
    {
        items.Remove(newItem);
    }

    public void UpdateUI()
    {
        //clean the inventory before opening it
        foreach (Transform child in itemsContent)
        {
            Destroy(child.gameObject);
        }

        foreach (item item in items)
        {
            GameObject newItem = Instantiate(inventoryItem, itemsContent);
            var itemName = newItem.transform.Find("ItemName").GetComponent<TMPro.TextMeshProUGUI>();
            var itemIcone = newItem.transform.Find("ItemIcon").GetComponent<UnityEngine.UI.Image>();

            itemName.text = item.itemName;
            itemIcone.sprite = item.icon;
        }
    }
}
