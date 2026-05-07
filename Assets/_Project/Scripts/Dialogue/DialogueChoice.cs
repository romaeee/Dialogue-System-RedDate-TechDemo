using System.Collections.Generic;

public sealed class DialogueChoice
{
    private readonly DialogueNode consequenceNode;
    private readonly List<RelationshipChange> relationshipChanges;

    public DialogueChoice(
        int lineNumber,
        string speakerName,
        string boxText,
        string selectedText,
        DialogueNode consequenceNode,
        string targetHubName = null,
        bool isOnce = false,
        List<RelationshipChange> relationshipChanges = null)
    {
        LineNumber = lineNumber;
        SpeakerName = speakerName;
        BoxText = boxText;
        SelectedText = selectedText;
        this.consequenceNode = consequenceNode;
        TargetHubName = targetHubName;
        IsOnce = isOnce;
        this.relationshipChanges = relationshipChanges ?? new List<RelationshipChange>();
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
}
