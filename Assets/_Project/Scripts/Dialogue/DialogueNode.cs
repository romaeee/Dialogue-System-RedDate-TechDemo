using System.Collections.Generic;

public sealed class DialogueNode
{
    private readonly List<DialogueElement> elements;

    public DialogueNode(string nodeName, List<DialogueElement> elements)
    {
        NodeName = nodeName;
        this.elements = elements;
    }

    public string NodeName { get; }
    public IReadOnlyList<DialogueElement> Elements => elements;
}
