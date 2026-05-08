using System.Collections.Generic;
using UnityEngine;

public static class DialogueValidator
{
    public static DialogueValidationReport Validate(
        TextAsset dialogueText,
        CharacterDatabase characterDatabase,
        BackgroundDatabase backgroundDatabase,
        ItemDatabase itemDatabase,
        RelationshipTypeDatabase relationshipTypeDatabase)
    {
        DialogueValidationReport report = new DialogueValidationReport();

        if (dialogueText == null)
        {
            report.Add(DialogueValidationSeverity.Error, 0, "Dialogue text asset is not assigned.");
            return report;
        }

        DialogueGraph graph = null;
        try
        {
            graph = DialogueParser.Parse(dialogueText);
        }
        catch (DialogueParseException exception)
        {
            report.Add(DialogueValidationSeverity.Error, exception.LineNumber, exception.Message);
            return report;
        }
        catch (System.Exception exception)
        {
            report.Add(DialogueValidationSeverity.Error, 0, exception.Message);
            return report;
        }

        ValidateDatabases(report, characterDatabase, backgroundDatabase, itemDatabase, relationshipTypeDatabase);
        ValidateGraph(report, graph, characterDatabase, backgroundDatabase, itemDatabase, relationshipTypeDatabase);
        return report;
    }

    private static void ValidateDatabases(
        DialogueValidationReport report,
        CharacterDatabase characterDatabase,
        BackgroundDatabase backgroundDatabase,
        ItemDatabase itemDatabase,
        RelationshipTypeDatabase relationshipTypeDatabase)
    {
        if (characterDatabase == null)
        {
            report.Add(DialogueValidationSeverity.Warning, 0, "Character database is not assigned; character references cannot be fully validated.");
        }

        if (backgroundDatabase == null)
        {
            report.Add(DialogueValidationSeverity.Warning, 0, "Background database is not assigned; background references cannot be fully validated.");
        }

        if (itemDatabase == null)
        {
            report.Add(DialogueValidationSeverity.Warning, 0, "Item database is not assigned; inventory references cannot be fully validated.");
        }

        if (relationshipTypeDatabase == null)
        {
            report.Add(DialogueValidationSeverity.Warning, 0, "Relationship type database is not assigned; relationship references cannot be fully validated.");
        }
    }

    private static void ValidateGraph(
        DialogueValidationReport report,
        DialogueGraph graph,
        CharacterDatabase characterDatabase,
        BackgroundDatabase backgroundDatabase,
        ItemDatabase itemDatabase,
        RelationshipTypeDatabase relationshipTypeDatabase)
    {
        Dictionary<string, DialogueHub> hubsByName = new Dictionary<string, DialogueHub>();
        HashSet<string> duplicateHubNames = new HashSet<string>();

        for (int i = 0; i < graph.Nodes.Count; i++)
        {
            DialogueNode node = graph.Nodes[i];
            for (int j = 0; j < node.Elements.Count; j++)
            {
                if (node.Elements[j] is DialogueHub hub)
                {
                    if (hubsByName.ContainsKey(hub.HubName))
                    {
                        duplicateHubNames.Add(hub.HubName);
                        report.Add(DialogueValidationSeverity.Error, hub.LineNumber, $"Duplicate hub name \"{hub.HubName}\".");
                    }
                    else
                    {
                        hubsByName.Add(hub.HubName, hub);
                    }
                }
            }
        }

        for (int i = 0; i < graph.Nodes.Count; i++)
        {
            DialogueNode node = graph.Nodes[i];
            for (int j = 0; j < node.Elements.Count; j++)
            {
                ValidateElement(report, node.Elements[j], hubsByName, duplicateHubNames, characterDatabase, backgroundDatabase, itemDatabase, relationshipTypeDatabase);
            }
        }
    }

    private static void ValidateElement(
        DialogueValidationReport report,
        DialogueElement element,
        Dictionary<string, DialogueHub> hubsByName,
        HashSet<string> duplicateHubNames,
        CharacterDatabase characterDatabase,
        BackgroundDatabase backgroundDatabase,
        ItemDatabase itemDatabase,
        RelationshipTypeDatabase relationshipTypeDatabase)
    {
        if (element is DialogueCommand command)
        {
            ValidateCommand(report, command, characterDatabase, backgroundDatabase);
            return;
        }

        if (element is DialogueLine line)
        {
            ValidateSpeaker(report, line.LineNumber, line.SpeakerName, characterDatabase);
            ValidateEffects(report, line.LineNumber, line.RelationshipChanges, line.RelationshipConditions, line.InventoryChanges, line.InventoryConditions, line.EmotionChanges, characterDatabase, itemDatabase, relationshipTypeDatabase);
            return;
        }

        if (element is DialogueHub hub)
        {
            ValidateHub(report, hub, hubsByName, duplicateHubNames, characterDatabase, itemDatabase, relationshipTypeDatabase);
        }
    }

    private static void ValidateCommand(
        DialogueValidationReport report,
        DialogueCommand command,
        CharacterDatabase characterDatabase,
        BackgroundDatabase backgroundDatabase)
    {
        switch (command.CommandType)
        {
            case DialogueCommandType.ShowCharacter:
            case DialogueCommandType.HideCharacter:
                ValidateCharacterReference(report, command.LineNumber, command.TargetName, characterDatabase, $"Command references unknown character \"{command.TargetName}\".");
                break;
            case DialogueCommandType.ShowBackground:
            case DialogueCommandType.HideBackground:
                if (backgroundDatabase != null && backgroundDatabase.GetByName(command.TargetName) == null)
                {
                    report.Add(DialogueValidationSeverity.Error, command.LineNumber, $"Command references unknown background \"{command.TargetName}\".");
                }
                break;
        }
    }

    private static void ValidateHub(
        DialogueValidationReport report,
        DialogueHub hub,
        Dictionary<string, DialogueHub> hubsByName,
        HashSet<string> duplicateHubNames,
        CharacterDatabase characterDatabase,
        ItemDatabase itemDatabase,
        RelationshipTypeDatabase relationshipTypeDatabase)
    {
        for (int i = 0; i < hub.Choices.Count; i++)
        {
            DialogueChoice choice = hub.Choices[i];
            if (choice.HasHubTarget)
            {
                if (!hubsByName.ContainsKey(choice.TargetHubName))
                {
                    report.Add(DialogueValidationSeverity.Error, choice.LineNumber, $"Choice points to missing hub \"{choice.TargetHubName}\".");
                }
                else if (duplicateHubNames.Contains(choice.TargetHubName))
                {
                    report.Add(DialogueValidationSeverity.Error, choice.LineNumber, $"Choice points to duplicate hub \"{choice.TargetHubName}\".");
                }
            }

            if (!string.IsNullOrWhiteSpace(choice.SpeakerName))
            {
                ValidateSpeaker(report, choice.LineNumber, choice.SpeakerName, characterDatabase);
            }

            ValidateEffects(report, choice.LineNumber, choice.RelationshipChanges, choice.RelationshipConditions, choice.InventoryChanges, choice.InventoryConditions, choice.EmotionChanges, characterDatabase, itemDatabase, relationshipTypeDatabase);
        }
    }

    private static void ValidateSpeaker(DialogueValidationReport report, int lineNumber, string speakerName, CharacterDatabase characterDatabase)
    {
        if (speakerName == "Player" || speakerName == "Narrator")
        {
            return;
        }

        ValidateCharacterReference(report, lineNumber, speakerName, characterDatabase, $"Dialogue speaker \"{speakerName}\" is not in the character database.");
    }

    private static void ValidateEffects(
        DialogueValidationReport report,
        int lineNumber,
        IReadOnlyList<RelationshipChange> relationshipChanges,
        IReadOnlyList<RelationshipCondition> relationshipConditions,
        IReadOnlyList<InventoryChange> inventoryChanges,
        IReadOnlyList<InventoryCondition> inventoryConditions,
        IReadOnlyList<CharacterEmotionChange> emotionChanges,
        CharacterDatabase characterDatabase,
        ItemDatabase itemDatabase,
        RelationshipTypeDatabase relationshipTypeDatabase)
    {
        ValidateRelationshipChanges(report, lineNumber, relationshipChanges, characterDatabase, relationshipTypeDatabase);
        ValidateRelationshipConditions(report, lineNumber, relationshipConditions, characterDatabase, relationshipTypeDatabase);
        ValidateInventoryChanges(report, lineNumber, inventoryChanges, itemDatabase);
        ValidateInventoryConditions(report, lineNumber, inventoryConditions, itemDatabase);
        ValidateEmotionChanges(report, lineNumber, emotionChanges, characterDatabase);
    }

    private static void ValidateRelationshipChanges(DialogueValidationReport report, int lineNumber, IReadOnlyList<RelationshipChange> changes, CharacterDatabase characterDatabase, RelationshipTypeDatabase relationshipTypeDatabase)
    {
        for (int i = 0; i < changes.Count; i++)
        {
            ValidateCharacterReference(report, lineNumber, changes[i].CharacterName, characterDatabase, $"Relationship change references unknown character \"{changes[i].CharacterName}\".");
            ValidateRelationshipTypeReference(report, lineNumber, changes[i].RelationshipTypeName, relationshipTypeDatabase);
        }
    }

    private static void ValidateRelationshipConditions(DialogueValidationReport report, int lineNumber, IReadOnlyList<RelationshipCondition> conditions, CharacterDatabase characterDatabase, RelationshipTypeDatabase relationshipTypeDatabase)
    {
        for (int i = 0; i < conditions.Count; i++)
        {
            ValidateCharacterReference(report, lineNumber, conditions[i].CharacterName, characterDatabase, $"Relationship condition references unknown character \"{conditions[i].CharacterName}\".");
            ValidateRelationshipTypeReference(report, lineNumber, conditions[i].RelationshipTypeName, relationshipTypeDatabase);
        }
    }

    private static void ValidateInventoryChanges(DialogueValidationReport report, int lineNumber, IReadOnlyList<InventoryChange> changes, ItemDatabase itemDatabase)
    {
        for (int i = 0; i < changes.Count; i++)
        {
            ValidateItemReference(report, lineNumber, changes[i].ItemName, itemDatabase);
        }
    }

    private static void ValidateInventoryConditions(DialogueValidationReport report, int lineNumber, IReadOnlyList<InventoryCondition> conditions, ItemDatabase itemDatabase)
    {
        for (int i = 0; i < conditions.Count; i++)
        {
            ValidateItemReference(report, lineNumber, conditions[i].ItemName, itemDatabase);
        }
    }

    private static void ValidateEmotionChanges(DialogueValidationReport report, int lineNumber, IReadOnlyList<CharacterEmotionChange> changes, CharacterDatabase characterDatabase)
    {
        for (int i = 0; i < changes.Count; i++)
        {
            CharacterEmotionChange change = changes[i];
            CharacterData character = characterDatabase != null ? characterDatabase.GetByName(change.CharacterName) : null;
            ValidateCharacterReference(report, lineNumber, change.CharacterName, characterDatabase, $"Emotion change references unknown character \"{change.CharacterName}\".");
            if (character != null && character.GetEmotionSprite(change.Emotion) == null)
            {
                report.Add(DialogueValidationSeverity.Warning, lineNumber, $"Character \"{change.CharacterName}\" has no sprite for emotion {change.Emotion}.");
            }
        }
    }

    private static void ValidateCharacterReference(DialogueValidationReport report, int lineNumber, string characterName, CharacterDatabase characterDatabase, string message)
    {
        if (characterDatabase != null && characterDatabase.GetByName(characterName) == null)
        {
            report.Add(DialogueValidationSeverity.Error, lineNumber, message);
        }
    }

    private static void ValidateRelationshipTypeReference(DialogueValidationReport report, int lineNumber, string relationshipTypeName, RelationshipTypeDatabase relationshipTypeDatabase)
    {
        if (relationshipTypeDatabase != null && relationshipTypeDatabase.GetByName(relationshipTypeName) == null)
        {
            report.Add(DialogueValidationSeverity.Error, lineNumber, $"Unknown relationship type \"{relationshipTypeName}\".");
        }
    }

    private static void ValidateItemReference(DialogueValidationReport report, int lineNumber, string itemName, ItemDatabase itemDatabase)
    {
        if (itemDatabase != null && itemDatabase.GetByName(itemName) == null)
        {
            report.Add(DialogueValidationSeverity.Error, lineNumber, $"Unknown inventory item \"{itemName}\".");
        }
    }
}
