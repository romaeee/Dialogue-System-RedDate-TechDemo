using UnityEngine;
using UnityEngine.UI;

public sealed class RelationshipBarView : MonoBehaviour
{
    [SerializeField] private Text typeLabel;
    [SerializeField] private Text valueLabel;
    [SerializeField] private Image fillImage;

    private Font fallbackFont;

    public void Setup(string relationshipTypeName, int value, int minValue, int maxValue)
    {
        if (typeLabel != null)
        {
            EnsureFont(typeLabel);
            typeLabel.text = relationshipTypeName;
        }

        if (valueLabel != null)
        {
            EnsureFont(valueLabel);
            valueLabel.text = value.ToString();
        }

        if (fillImage != null)
        {
            RectTransform fillTransform = fillImage.GetComponent<RectTransform>();
            float normalizedValue = Mathf.Clamp01(GetNormalizedValue(value, minValue, maxValue));
            fillTransform.anchorMin = Vector2.zero;
            fillTransform.anchorMax = new Vector2(normalizedValue, 1f);
            fillTransform.offsetMin = Vector2.zero;
            fillTransform.offsetMax = Vector2.zero;
        }
    }

    private static float GetNormalizedValue(int value, int minValue, int maxValue)
    {
        if (maxValue <= 0)
        {
            return 0f;
        }

        return Mathf.InverseLerp(Mathf.Max(0, minValue), maxValue, Mathf.Max(0, value));
    }

    private void EnsureFont(Text text)
    {
        if (text.font != null)
        {
            return;
        }

        if (fallbackFont == null)
        {
            fallbackFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (fallbackFont == null)
            {
                fallbackFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
        }

        text.font = fallbackFont;
    }
}
