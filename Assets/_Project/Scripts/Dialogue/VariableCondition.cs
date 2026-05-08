public sealed class VariableCondition
{
    public VariableCondition(string variableName, string comparisonOperator, string value)
    {
        VariableName = variableName;
        ComparisonOperator = comparisonOperator;
        Value = value;
    }

    public string VariableName { get; }
    public string ComparisonOperator { get; }
    public string Value { get; }
}
