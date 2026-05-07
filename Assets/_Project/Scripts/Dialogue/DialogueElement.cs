public abstract class DialogueElement
{
    protected DialogueElement(int lineNumber)
    {
        LineNumber = lineNumber;
    }

    public int LineNumber { get; }
}
