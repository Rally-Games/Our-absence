using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Item/create new Item")]
public class item : ScriptableObject
{
    public int id;
    public string itemName;
    public int value;
    public Sprite icon;
}
