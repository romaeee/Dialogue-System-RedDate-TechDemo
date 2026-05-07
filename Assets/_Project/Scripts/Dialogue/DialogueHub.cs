using System.Collections.Generic;

public sealed class DialogueHub : DialogueElement
{
    private readonly List<DialogueChoice> choices;

    public DialogueHub(int lineNumber, string hubName, List<DialogueChoice> choices) : base(lineNumber)
    {
        HubName = hubName;
        this.choices = choices;
    }

    public string HubName { get; }
    public IReadOnlyList<DialogueChoice> Choices => choices;
}
