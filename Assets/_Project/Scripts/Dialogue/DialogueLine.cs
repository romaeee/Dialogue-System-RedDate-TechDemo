public sealed class DialogueLine : DialogueElement
{
    public DialogueLine(int lineNumber, string speakerName, string text) : base(lineNumber)
    {
        SpeakerName = speakerName;
        Text = text;
    }

    public string SpeakerName { get; }
    public string Text { get; }
}
