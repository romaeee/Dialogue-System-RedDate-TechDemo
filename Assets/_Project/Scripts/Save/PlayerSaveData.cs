using System;
using System.Collections.Generic;

[Serializable]
public sealed class PlayerSaveData
{
    public List<RelationshipValueSaveData> relationships = new List<RelationshipValueSaveData>();
    public List<InventoryItemSaveData> inventoryItems = new List<InventoryItemSaveData>();
    public List<VariableValueSaveData> variables = new List<VariableValueSaveData>();
}
