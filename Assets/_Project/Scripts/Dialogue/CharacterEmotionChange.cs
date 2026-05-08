public sealed class CharacterEmotionChange
{
    public CharacterEmotionChange(string characterName, CharacterEmotion emotion)
    {
        CharacterName = characterName;
        Emotion = emotion;
    }

    public string CharacterName { get; }
    public CharacterEmotion Emotion { get; }
}
