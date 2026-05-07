using System.Collections.Generic;

public sealed class DialogueChoice
{
    private readonly DialogueNode consequenceNode;
    private readonly List<RelationshipChange> relationshipChanges;
    private readonly List<RelationshipCondition> relationshipConditions;
    private readonly List<InventoryChange> inventoryChanges;
    private readonly List<InventoryCondition> inventoryConditions;

    public DialogueChoice(
        int lineNumber,
        string speakerName,
        string boxText,
        string selectedText,
        DialogueNode consequenceNode,
        string targetHubName = null,
        bool isOnce = false,
        List<RelationshipChange> relationshipChanges = null,
        List<RelationshipCondition> relationshipConditions = null,
        List<InventoryChange> inventoryChanges = null,
        List<InventoryCondition> inventoryConditions = null)
    {
        LineNumber = lineNumber;
        SpeakerName = speakerName;
        BoxText = boxText;
        SelectedText = selectedText;
        this.consequenceNode = consequenceNode;
        TargetHubName = targetHubName;
        IsOnce = isOnce;
        this.relationshipChanges = relationshipChanges ?? new List<RelationshipChange>();
        this.relationshipConditions = relationshipConditions ?? new List<RelationshipCondition>();
        this.inventoryChanges = inventoryChanges ?? new List<InventoryChange>();
        this.inventoryConditions = inventoryConditions ?? new List<InventoryCondition>();
    }

    public int LineNumber { get; }
    public string SpeakerName { get; }
    public string BoxText { get; }
    public string SelectedText { get; }
    public DialogueNode ConsequenceNode => consequenceNode;
    public string TargetHubName { get; }
    public bool IsOnce { get; }
    public bool HasHubTarget => !string.IsNullOrWhiteSpace(TargetHubName);
    public IReadOnlyList<RelationshipChange> RelationshipChanges => relationshipChanges;
    public IReadOnlyList<RelationshipCondition> RelationshipConditions => relationshipConditions;
    public IReadOnlyList<InventoryChange> InventoryChanges => inventoryChanges;
    public IReadOnlyList<InventoryCondition> InventoryConditions => inventoryConditions;
}
