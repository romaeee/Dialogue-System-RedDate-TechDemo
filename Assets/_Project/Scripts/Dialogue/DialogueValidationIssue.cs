public sealed class DialogueValidationIssue
{
    public DialogueValidationIssue(DialogueValidationSeverity severity, int lineNumber, string message)
    {
        Severity = severity;
        LineNumber = lineNumber;
        Message = message;
    }

    public DialogueValidationSeverity Severity { get; }
    public int LineNumber { get; }
    public string Message { get; }

    public override string ToString()
    {
        string location = LineNumber > 0 ? $"Line {LineNumber}: " : string.Empty;
        return $"[{Severity}] {location}{Message}";
    }
}
