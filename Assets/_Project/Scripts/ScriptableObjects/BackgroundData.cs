using UnityEngine;

[CreateAssetMenu(fileName = "New Background", menuName = "Red Date/Dialogue/Background")]
public sealed class BackgroundData : ScriptableObject
{
    [SerializeField] private string backgroundName;
    [SerializeField] private Sprite image;

    public string BackgroundName => backgroundName;
    public Sprite Image => image;
}
