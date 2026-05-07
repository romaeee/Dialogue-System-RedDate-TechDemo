using System.Collections.Generic;

public sealed class DialogueLine : DialogueElement
{
    private readonly List<RelationshipChange> relationshipChanges;
    private readonly List<RelationshipCondition> relationshipConditions;
    private readonly List<InventoryChange> inventoryChanges;
    private readonly List<InventoryCondition> inventoryConditions;

    public DialogueLine(
        int lineNumber,
        string speakerName,
        string text,
        List<RelationshipChange> relationshipChanges = null,
        List<RelationshipCondition> relationshipConditions = null,
        List<InventoryChange> inventoryChanges = null,
        List<InventoryCondition> inventoryConditions = null) : base(lineNumber)
    {
        SpeakerName = speakerName;
        Text = text;
        this.relationshipChanges = relationshipChanges ?? new List<RelationshipChange>();
        this.relationshipConditions = relationshipConditions ?? new List<RelationshipCondition>();
        this.inventoryChanges = inventoryChanges ?? new List<InventoryChange>();
        this.inventoryConditions = inventoryConditions ?? new List<InventoryCondition>();
    }

    public string SpeakerName { get; }
    public string Text { get; }
    public IReadOnlyList<RelationshipChange> RelationshipChanges => relationshipChanges;
    public IReadOnlyList<RelationshipCondition> RelationshipConditions => relationshipConditions;
    public IReadOnlyList<InventoryChange> InventoryChanges => inventoryChanges;
    public IReadOnlyList<InventoryCondition> InventoryConditions => inventoryConditions;
}
