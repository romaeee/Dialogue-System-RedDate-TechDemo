using System;
using UnityEngine;

[Serializable]
public sealed class CharacterEmotionSprite
{
    [SerializeField] private CharacterEmotion emotion = CharacterEmotion.Normal;
    [SerializeField] private Sprite image;

    public CharacterEmotion Emotion => emotion;
    public Sprite Image => image;

    public void SetEmotion(CharacterEmotion value)
    {
        emotion = value;
    }
}
