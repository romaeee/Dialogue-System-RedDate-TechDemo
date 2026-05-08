using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Character", menuName = "Red Date/Dialogue/Character")]
public sealed class CharacterData : ScriptableObject
{
    [SerializeField] private string characterName;
    [SerializeField] private List<CharacterEmotionSprite> emotions = new List<CharacterEmotionSprite>();
    [SerializeField] private Sprite portrait;

    public string CharacterName => characterName;
    public IReadOnlyList<CharacterEmotionSprite> Emotions => emotions;
    public Sprite Portrait => portrait;
    public Sprite RelationshipPortrait => portrait != null ? portrait : GetEmotionSprite(CharacterEmotion.Normal);

    public Sprite GetEmotionSprite(CharacterEmotion emotion)
    {
        if (emotions == null || emotions.Count == 0)
        {
            return null;
        }

        for (int i = 0; i < emotions.Count; i++)
        {
            CharacterEmotionSprite emotionSprite = emotions[i];
            if (emotionSprite != null && emotionSprite.Emotion == emotion && emotionSprite.Image != null)
            {
                return emotionSprite.Image;
            }
        }

        return emotions[0] != null ? emotions[0].Image : null;
    }

    private void OnValidate()
    {
        if (emotions == null)
        {
            emotions = new List<CharacterEmotionSprite>();
        }

        if (emotions.Count == 0)
        {
            emotions.Add(new CharacterEmotionSprite());
        }

        if (emotions[0] != null)
        {
            emotions[0].SetEmotion(CharacterEmotion.Normal);
        }
    }
}
