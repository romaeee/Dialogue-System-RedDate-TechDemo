using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Item Database", menuName = "Red Date/Inventory/Item Database")]
public sealed class ItemDatabase : ScriptableObject
{
    [SerializeField] private List<ItemData> items = new List<ItemData>();

    public IReadOnlyList<ItemData> Items => items;

    public ItemData GetByName(string itemName)
    {
        for (int i = 0; i < items.Count; i++)
        {
            ItemData item = items[i];
            if (item != null && item.ItemName == itemName)
            {
                return item;
            }
        }

        return null;
    }
}
