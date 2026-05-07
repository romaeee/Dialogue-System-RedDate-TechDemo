using System.IO;
using System.Threading.Tasks;
using UnityEngine;

public static class SaveSystem
{
    public const int CurrentVersion = 1;

    public static string GetSavePath(string fileName)
    {
        return Path.Combine(Application.persistentDataPath, fileName);
    }

    public static async Task SaveAsync(string fileName, SaveGameData saveData)
    {
        if (saveData == null)
        {
            Debug.LogError("Cannot save null save data.");
            return;
        }

        saveData.version = CurrentVersion;
        string savePath = GetSavePath(fileName);
        string directoryPath = Path.GetDirectoryName(savePath);

        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        string json = JsonUtility.ToJson(saveData, true);
        await Task.Run(() => File.WriteAllText(savePath, json));
        Debug.Log($"Saved game to {savePath}");
    }

    public static async Task<SaveGameData> LoadAsync(string fileName)
    {
        string savePath = GetSavePath(fileName);

        if (!File.Exists(savePath))
        {
            Debug.LogWarning($"Save file does not exist: {savePath}");
            return null;
        }

        string json = await Task.Run(() => File.ReadAllText(savePath));
        json = SaveMigrationSystem.MigrateToCurrentVersion(json, CurrentVersion);
        return JsonUtility.FromJson<SaveGameData>(json);
    }

    public static async Task DeleteAsync(string fileName)
    {
        string savePath = GetSavePath(fileName);

        if (!File.Exists(savePath))
        {
            Debug.LogWarning($"Save file does not exist: {savePath}");
            return;
        }

        await Task.Run(() => File.Delete(savePath));
        Debug.Log($"Deleted save file: {savePath}");
    }
}
