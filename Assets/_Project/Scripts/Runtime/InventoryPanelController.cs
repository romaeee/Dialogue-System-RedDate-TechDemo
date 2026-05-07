using UnityEngine;
using UnityEngine.UI;

public sealed class InventoryPanelController : MonoBehaviour
{
    [SerializeField] private Button invButton;
    [SerializeField] private PlayerController playerController;

    private Font font;
    private GameObject panelRoot;
    private RectTransform contentRoot;

    private void Start()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        if (invButton != null)
        {
            invButton.onClick.RemoveListener(TogglePanel);
            invButton.onClick.AddListener(TogglePanel);
        }
        else
        {
            Debug.LogWarning("Inventory button is not assigned.");
        }
    }

    private void OnDestroy()
    {
        if (invButton != null)
        {
            invButton.onClick.RemoveListener(TogglePanel);
        }
    }

    public void TogglePanel()
    {
        EnsurePanel();

        bool shouldShow = !panelRoot.activeSelf;
        panelRoot.SetActive(shouldShow);

        if (shouldShow)
        {
            Refresh();
        }
    }

    public void Refresh()
    {
        EnsurePanel();
        ClearContent();

        CreateText("Inventory", 34, TextAnchor.MiddleLeft, 46f);

        if (playerController == null)
        {
            CreateText("Player controller missing", 26, TextAnchor.MiddleLeft, 40f);
            return;
        }

        int itemCount = 0;
        foreach (string itemName in playerController.InventoryItems)
        {
            CreateText(itemName, 28, TextAnchor.MiddleLeft, 38f);
            itemCount++;
        }

        if (itemCount == 0)
        {
            CreateText("Empty", 28, TextAnchor.MiddleLeft, 38f);
        }
    }

    private void EnsurePanel()
    {
        if (panelRoot != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject("Inventory UI");
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 120;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        panelRoot = new GameObject("Inventory Panel");
        panelRoot.transform.SetParent(canvasObject.transform, false);

        RectTransform panelTransform = panelRoot.AddComponent<RectTransform>();
        panelTransform.anchorMin = new Vector2(1f, 1f);
        panelTransform.anchorMax = new Vector2(1f, 1f);
        panelTransform.pivot = new Vector2(1f, 1f);
        panelTransform.anchoredPosition = new Vector2(-48f, -170f);
        panelTransform.sizeDelta = new Vector2(360f, 420f);

        Image image = panelRoot.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.82f);

        GameObject contentObject = new GameObject("Inventory Content");
        contentObject.transform.SetParent(panelRoot.transform, false);
        contentRoot = contentObject.AddComponent<RectTransform>();
        contentRoot.anchorMin = Vector2.zero;
        contentRoot.anchorMax = Vector2.one;
        contentRoot.offsetMin = new Vector2(24f, 24f);
        contentRoot.offsetMax = new Vector2(-24f, -24f);

        VerticalLayoutGroup layoutGroup = contentObject.AddComponent<VerticalLayoutGroup>();
        layoutGroup.spacing = 10f;
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = false;

        panelRoot.SetActive(false);
    }

    private void ClearContent()
    {
        for (int i = contentRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(contentRoot.GetChild(i).gameObject);
        }
    }

    private void CreateText(string textValue, int fontSize, TextAnchor alignment, float preferredHeight)
    {
        GameObject textObject = new GameObject("Text");
        textObject.transform.SetParent(contentRoot, false);

        Text text = textObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.text = textValue;

        LayoutElement layoutElement = textObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = preferredHeight;
    }
}
