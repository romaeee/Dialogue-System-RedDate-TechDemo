public sealed class RelationshipCondition
{
    public RelationshipCondition(string relationshipTypeName, string characterName, string comparisonOperator, int value)
    {
        RelationshipTypeName = relationshipTypeName;
        CharacterName = characterName;
        ComparisonOperator = comparisonOperator;
        Value = value;
    }

    public string RelationshipTypeName { get; }
    public string CharacterName { get; }
    public string ComparisonOperator { get; }
    public int Value { get; }
}
