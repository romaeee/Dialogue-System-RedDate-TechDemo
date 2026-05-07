public sealed class InventoryChange
{
    public InventoryChange(string itemName, bool shouldAdd)
    {
        ItemName = itemName;
        ShouldAdd = shouldAdd;
    }

    public string ItemName { get; }
    public bool ShouldAdd { get; }
}
