public sealed class InventoryCondition
{
    public InventoryCondition(string itemName, bool shouldHaveItem)
    {
        ItemName = itemName;
        ShouldHaveItem = shouldHaveItem;
    }

    public string ItemName { get; }
    public bool ShouldHaveItem { get; }
}
