using System.Collections.Generic;
using UnityEngine;

public sealed class PlayerController : MonoBehaviour, ISavable<PlayerSaveData>
{
    [SerializeField] private CharacterDatabase characterDatabase;
    [SerializeField] private RelationshipTypeDatabase relationshipTypeDatabase;

    private readonly Dictionary<string, int> relationships = new Dictionary<string, int>();

    public void ApplyRelationshipChange(RelationshipChange change)
    {
        if (change == null)
        {
            return;
        }

        if (!IsKnownCharacter(change.CharacterName))
        {
            Debug.LogWarning($"Relationship target character \"{change.CharacterName}\" was not found.");
        }

        if (!IsKnownRelationshipType(change.RelationshipTypeName))
        {
            Debug.LogWarning($"Relationship type \"{change.RelationshipTypeName}\" was not found.");
        }

        string key = BuildKey(change.CharacterName, change.RelationshipTypeName);
        relationships.TryGetValue(key, out int currentValue);
        int newValue = currentValue + change.Delta;
        relationships[key] = newValue;
        Debug.Log($"[Relationship] {change.CharacterName} {change.RelationshipTypeName}: {newValue} ({change.Delta:+#;-#;0})");
    }

    public int GetRelationshipValue(string characterName, string relationshipTypeName)
    {
        relationships.TryGetValue(BuildKey(characterName, relationshipTypeName), out int value);
        return value;
    }

    public PlayerSaveData CaptureState()
    {
        PlayerSaveData state = new PlayerSaveData();

        foreach (KeyValuePair<string, int> relationship in relationships)
        {
            SplitKey(relationship.Key, out string characterName, out string relationshipTypeName);
            state.relationships.Add(new RelationshipValueSaveData
            {
                characterName = characterName,
                relationshipTypeName = relationshipTypeName,
                value = relationship.Value
            });
        }

        return state;
    }

    public void RestoreState(PlayerSaveData state)
    {
        relationships.Clear();

        if (state == null)
        {
            return;
        }

        for (int i = 0; i < state.relationships.Count; i++)
        {
            RelationshipValueSaveData relationship = state.relationships[i];
            relationships[BuildKey(relationship.characterName, relationship.relationshipTypeName)] = relationship.value;
        }
    }

    private bool IsKnownCharacter(string characterName)
    {
        return characterDatabase == null || characterDatabase.GetByName(characterName) != null;
    }

    private bool IsKnownRelationshipType(string relationshipTypeName)
    {
        return relationshipTypeDatabase == null || relationshipTypeDatabase.GetByName(relationshipTypeName) != null;
    }

    private static string BuildKey(string characterName, string relationshipTypeName)
    {
        return $"{characterName}::{relationshipTypeName}";
    }

    private static void SplitKey(string key, out string characterName, out string relationshipTypeName)
    {
        string[] parts = key.Split(new[] { "::" }, System.StringSplitOptions.None);
        characterName = parts.Length > 0 ? parts[0] : string.Empty;
        relationshipTypeName = parts.Length > 1 ? parts[1] : string.Empty;
    }
}
