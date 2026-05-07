using System;

public sealed class DialogueParseException : Exception
{
    public DialogueParseException(int lineNumber, string message)
        : base($"Dialogue parse error on line {lineNumber}: {message}")
    {
        LineNumber = lineNumber;
    }

    public int LineNumber { get; }
}
