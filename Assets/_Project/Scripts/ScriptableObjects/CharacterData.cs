using UnityEngine;

[CreateAssetMenu(fileName = "New Character", menuName = "Red Date/Dialogue/Character")]
public sealed class CharacterData : ScriptableObject
{
    [SerializeField] private string characterName;
    [SerializeField] private Sprite image;
    [SerializeField] private Sprite portrait;

    public string CharacterName => characterName;
    public Sprite Image => image;
    public Sprite Portrait => portrait;
    public Sprite RelationshipPortrait => portrait != null ? portrait : image;
}
