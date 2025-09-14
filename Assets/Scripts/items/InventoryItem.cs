using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "New Item", menuName = "Item/create new Item")]
public class InventoryItem : ScriptableObject
{
    public string itemName;
    public MainMenuController.ItemType itemSubType;
    public MainMenuController.CategoryType itemCategory;
    public int itemID;
    public Sprite icon;  // For now not in use
    public int value;
    public GameObject objectRef;

    public override string ToString()
    {
        return $"{itemName} ({itemSubType})";
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
