using UnityEngine;

public sealed class FrameRateController : MonoBehaviour
{
    private const int MinFrameRate = 30;
    private const int MaxFrameRate = 120;
    private const int DefaultFrameRate = 30;

    [SerializeField, Range(MinFrameRate, MaxFrameRate)] private int targetFrameRate = DefaultFrameRate;
    [SerializeField] private bool disableVSync = true;
    [SerializeField] private bool runInBackground;

    private void Awake()
    {
        Apply();
    }

    private void OnValidate()
    {
        targetFrameRate = Mathf.Clamp(targetFrameRate, MinFrameRate, MaxFrameRate);

        if (Application.isPlaying)
        {
            Apply();
        }
    }

    public void SetTargetFrameRate(int frameRate)
    {
        targetFrameRate = Mathf.Clamp(frameRate, MinFrameRate, MaxFrameRate);
        Apply();
    }

    private void Apply()
    {
        if (disableVSync)
        {
            QualitySettings.vSyncCount = 0;
        }

        Application.targetFrameRate = targetFrameRate;
        Application.runInBackground = runInBackground;
    }
}
