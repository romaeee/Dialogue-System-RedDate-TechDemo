public sealed class DialogueCommand : DialogueElement
{
    public DialogueCommand(int lineNumber, DialogueCommandType commandType, string targetName) : base(lineNumber)
    {
        CommandType = commandType;
        TargetName = targetName;
    }

    public DialogueCommandType CommandType { get; }
    public string TargetName { get; }
}
