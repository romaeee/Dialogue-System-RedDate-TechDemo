using TMPro;
using UnityEngine;

public sealed class ScreenTextView : MonoBehaviour
{
    [SerializeField] private TMP_Text text;

    public TMP_Text Text => text;

    private void Awake()
    {
        AutoBind();
    }

    private void OnValidate()
    {
        AutoBind();
    }

    public void SetText(string value)
    {
        AutoBind();

        if (text == null)
        {
            return;
        }

        text.text = value;
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void AutoBind()
    {
        if (text == null)
        {
            text = GetComponentInChildren<TMP_Text>(true);
        }
    }
}
