public sealed class VariableChange
{
    public VariableChange(string variableName, string value)
    {
        VariableName = variableName;
        Value = value;
    }

    public string VariableName { get; }
    public string Value { get; }
}
