using UnityEngine;

[CreateAssetMenu(fileName = "New Character", menuName = "Red Date/Dialogue/Character")]
public sealed class CharacterData : ScriptableObject
{
    [SerializeField] private string characterName;
    [SerializeField] private Sprite image;

    public string CharacterName => characterName;
    public Sprite Image => image;
}
