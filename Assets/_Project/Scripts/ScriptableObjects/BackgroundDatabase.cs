using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Background Database", menuName = "Red Date/Dialogue/Background Database")]
public sealed class BackgroundDatabase : ScriptableObject
{
    [SerializeField] private List<BackgroundData> backgrounds = new List<BackgroundData>();

    public IReadOnlyList<BackgroundData> Backgrounds => backgrounds;

    public BackgroundData GetByName(string backgroundName)
    {
        if (string.IsNullOrWhiteSpace(backgroundName))
        {
            return null;
        }

        for (int i = 0; i < backgrounds.Count; i++)
        {
            BackgroundData background = backgrounds[i];
            if (background != null && background.BackgroundName == backgroundName)
            {
                return background;
            }
        }

        return null;
    }
}
