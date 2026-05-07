using System;

[Serializable]
public sealed class RelationshipChange
{
    public RelationshipChange(string relationshipTypeName, string characterName, int delta)
    {
        RelationshipTypeName = relationshipTypeName;
        CharacterName = characterName;
        Delta = delta;
    }

    public string RelationshipTypeName { get; }
    public string CharacterName { get; }
    public int Delta { get; }
}
