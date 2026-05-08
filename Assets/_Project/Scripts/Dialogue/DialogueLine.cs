using System.Collections.Generic;

public sealed class DialogueLine : DialogueElement
{
    private readonly List<RelationshipChange> relationshipChanges;
    private readonly List<RelationshipCondition> relationshipConditions;
    private readonly List<InventoryChange> inventoryChanges;
    private readonly List<InventoryCondition> inventoryConditions;
    private readonly List<VariableChange> variableChanges;
    private readonly List<VariableCondition> variableConditions;

    public DialogueLine(
        int lineNumber,
        string speakerName,
        string text,
        List<RelationshipChange> relationshipChanges = null,
        List<RelationshipCondition> relationshipConditions = null,
        List<InventoryChange> inventoryChanges = null,
        List<InventoryCondition> inventoryConditions = null,
        List<VariableChange> variableChanges = null,
        List<VariableCondition> variableConditions = null) : base(lineNumber)
    {
        SpeakerName = speakerName;
        Text = text;
        this.relationshipChanges = relationshipChanges ?? new List<RelationshipChange>();
        this.relationshipConditions = relationshipConditions ?? new List<RelationshipCondition>();
        this.inventoryChanges = inventoryChanges ?? new List<InventoryChange>();
        this.inventoryConditions = inventoryConditions ?? new List<InventoryCondition>();
        this.variableChanges = variableChanges ?? new List<VariableChange>();
        this.variableConditions = variableConditions ?? new List<VariableCondition>();
    }

    public string SpeakerName { get; }
    public string Text { get; }
    public IReadOnlyList<RelationshipChange> RelationshipChanges => relationshipChanges;
    public IReadOnlyList<RelationshipCondition> RelationshipConditions => relationshipConditions;
    public IReadOnlyList<InventoryChange> InventoryChanges => inventoryChanges;
    public IReadOnlyList<InventoryCondition> InventoryConditions => inventoryConditions;
    public IReadOnlyList<VariableChange> VariableChanges => variableChanges;
    public IReadOnlyList<VariableCondition> VariableConditions => variableConditions;
}
