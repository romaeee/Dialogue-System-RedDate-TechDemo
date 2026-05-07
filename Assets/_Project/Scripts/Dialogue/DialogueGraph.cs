using System.Collections.Generic;

public sealed class DialogueGraph
{
    private readonly List<DialogueNode> nodes;

    public DialogueGraph(List<DialogueNode> nodes)
    {
        this.nodes = nodes;
    }

    public IReadOnlyList<DialogueNode> Nodes => nodes;
    public DialogueNode StartNode => nodes.Count > 0 ? nodes[0] : null;
}
