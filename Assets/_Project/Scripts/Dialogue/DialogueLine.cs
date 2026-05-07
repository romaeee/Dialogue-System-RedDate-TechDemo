using System.Collections.Generic;

public sealed class DialogueLine : DialogueElement
{
    private readonly List<RelationshipChange> relationshipChanges;

    public DialogueLine(
        int lineNumber,
        string speakerName,
        string text,
        List<RelationshipChange> relationshipChanges = null) : base(lineNumber)
    {
        SpeakerName = speakerName;
        Text = text;
        this.relationshipChanges = relationshipChanges ?? new List<RelationshipChange>();
    }

    public string SpeakerName { get; }
    public string Text { get; }
    public IReadOnlyList<RelationshipChange> RelationshipChanges => relationshipChanges;
}
