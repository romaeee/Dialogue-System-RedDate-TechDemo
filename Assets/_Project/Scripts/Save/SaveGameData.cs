using System;

[Serializable]
public sealed class SaveGameData
{
    public int version = SaveSystem.CurrentVersion;
    public DialogueSaveData dialogue = new DialogueSaveData();
    public PlayerSaveData player = new PlayerSaveData();
}
