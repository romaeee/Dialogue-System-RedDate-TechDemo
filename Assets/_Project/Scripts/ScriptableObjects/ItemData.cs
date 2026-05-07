using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Red Date/Inventory/Item")]
public sealed class ItemData : ScriptableObject
{
    [SerializeField] private string itemName;
    [SerializeField] private Sprite image;

    public string ItemName => itemName;
    public Sprite Image => image;
}
