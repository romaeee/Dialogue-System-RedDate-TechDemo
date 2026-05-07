using System.Collections.Generic;
using UnityEngine;

public sealed class PlayerController : MonoBehaviour, ISavable<PlayerSaveData>
{
    [SerializeField] private CharacterDatabase characterDatabase;
    [SerializeField] private RelationshipTypeDatabase relationshipTypeDatabase;
    [SerializeField] private ItemDatabase itemDatabase;
    [SerializeField] private bool verboseLogging;

    private readonly Dictionary<string, int> relationships = new Dictionary<string, int>();
    private readonly HashSet<string> inventoryItems = new HashSet<string>();

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
        if (verboseLogging)
        {
            Debug.Log($"[Relationship] {change.CharacterName} {change.RelationshipTypeName}: {newValue} ({change.Delta:+#;-#;0})");
        }
    }

    public int GetRelationshipValue(string characterName, string relationshipTypeName)
    {
        relationships.TryGetValue(BuildKey(characterName, relationshipTypeName), out int value);
        return value;
    }

    public void ApplyInventoryChange(InventoryChange change)
    {
        if (change == null)
        {
            return;
        }

        if (!IsKnownItem(change.ItemName))
        {
            Debug.LogWarning($"Inventory item \"{change.ItemName}\" was not found.");
        }

        if (change.ShouldAdd)
        {
            inventoryItems.Add(change.ItemName);
            if (verboseLogging)
            {
                Debug.Log($"[Inventory] Got item: {change.ItemName}");
            }
        }
        else
        {
            inventoryItems.Remove(change.ItemName);
            if (verboseLogging)
            {
                Debug.Log($"[Inventory] Lost item: {change.ItemName}");
            }
        }
    }

    public bool HasItem(string itemName)
    {
        return inventoryItems.Contains(itemName);
    }

    public IReadOnlyCollection<string> InventoryItems => inventoryItems;

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

        foreach (string itemName in inventoryItems)
        {
            state.inventoryItems.Add(new InventoryItemSaveData
            {
                itemName = itemName
            });
        }

        return state;
    }

    public void RestoreState(PlayerSaveData state)
    {
        relationships.Clear();
        inventoryItems.Clear();

        if (state == null)
        {
            return;
        }

        for (int i = 0; i < state.relationships.Count; i++)
        {
            RelationshipValueSaveData relationship = state.relationships[i];
            relationships[BuildKey(relationship.characterName, relationship.relationshipTypeName)] = relationship.value;
        }

        if (state.inventoryItems == null)
        {
            return;
        }

        for (int i = 0; i < state.inventoryItems.Count; i++)
        {
            InventoryItemSaveData item = state.inventoryItems[i];
            if (!string.IsNullOrWhiteSpace(item.itemName))
            {
                inventoryItems.Add(item.itemName);
            }
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

    private bool IsKnownItem(string itemName)
    {
        return itemDatabase == null || itemDatabase.GetByName(itemName) != null;
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
