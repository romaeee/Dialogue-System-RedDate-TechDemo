using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Character Database", menuName = "Red Date/Dialogue/Character Database")]
public sealed class CharacterDatabase : ScriptableObject
{
    [SerializeField] private List<CharacterData> characters = new List<CharacterData>();

    public IReadOnlyList<CharacterData> Characters => characters;

    public CharacterData GetByName(string characterName)
    {
        if (string.IsNullOrWhiteSpace(characterName))
        {
            return null;
        }

        for (int i = 0; i < characters.Count; i++)
        {
            CharacterData character = characters[i];
            if (character != null && character.CharacterName == characterName)
            {
                return character;
            }
        }

        return null;
    }
}
