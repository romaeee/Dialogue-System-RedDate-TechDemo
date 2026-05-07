using UnityEngine;
using UnityEngine.UI;

public sealed class RelationshipPanelController : MonoBehaviour
{
    [SerializeField] private Button relButton;
    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private CharacterDatabase characterDatabase;
    [SerializeField] private RelationshipTypeDatabase relationshipTypeDatabase;
    [SerializeField] private RelationshipCardView relationshipCardPrefab;
    [SerializeField] private RelationshipBarView relationshipBarPrefab;
    [SerializeField] private int minRelationshipValue = -10;
    [SerializeField] private int maxRelationshipValue = 10;

    private Font font;
    private RectTransform contentRoot;

    private void Start()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        if (relButton != null)
        {
            relButton.onClick.RemoveListener(TogglePanel);
            relButton.onClick.AddListener(TogglePanel);
        }

        if (panelRoot != null)
        {
            panelRoot.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (relButton != null)
        {
            relButton.onClick.RemoveListener(TogglePanel);
        }
    }

    public void TogglePanel()
    {
        if (panelRoot == null)
        {
            Debug.LogWarning("Relationship panel root is not assigned.");
            return;
        }

        bool shouldShow = !panelRoot.gameObject.activeSelf;
        panelRoot.gameObject.SetActive(shouldShow);

        if (shouldShow)
        {
            Refresh();
        }
    }

    public void Refresh()
    {
        if (panelRoot == null ||
            characterDatabase == null ||
            relationshipTypeDatabase == null ||
            playerController == null ||
            relationshipCardPrefab == null ||
            relationshipBarPrefab == null)
        {
            Debug.LogWarning("Relationship panel is missing required references.");
            return;
        }

        EnsureContentRoot();
        ClearContent();
        CreateTitle();

        for (int i = 0; i < characterDatabase.Characters.Count; i++)
        {
            CharacterData character = characterDatabase.Characters[i];
            if (character != null)
            {
                RelationshipCardView card = Instantiate(relationshipCardPrefab, contentRoot);
                card.Setup(
                    character,
                    relationshipTypeDatabase,
                    playerController,
                    relationshipBarPrefab,
                    minRelationshipValue,
                    maxRelationshipValue);
            }
        }
    }

    private void EnsureContentRoot()
    {
        if (contentRoot != null)
        {
            return;
        }

        Image panelImage = panelRoot.GetComponent<Image>();
        if (panelImage != null)
        {
            panelImage.enabled = true;
            panelImage.color = new Color(0f, 0f, 0f, 0f);
        }

        GameObject contentObject = new GameObject("Relationship Content");
        contentObject.transform.SetParent(panelRoot, false);
        contentRoot = contentObject.AddComponent<RectTransform>();
        contentRoot.anchorMin = new Vector2(0.5f, 0.5f);
        contentRoot.anchorMax = new Vector2(0.5f, 0.5f);
        contentRoot.pivot = new Vector2(0.5f, 0.5f);
        contentRoot.sizeDelta = new Vector2(760f, 600f);

        VerticalLayoutGroup layoutGroup = contentObject.AddComponent<VerticalLayoutGroup>();
        layoutGroup.spacing = 18f;
        layoutGroup.padding = new RectOffset(24, 24, 24, 24);
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = false;
    }

    private void ClearContent()
    {
        for (int i = contentRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(contentRoot.GetChild(i).gameObject);
        }
    }

    private void CreateTitle()
    {
        Text title = CreateText("Relationships", contentRoot, 34, TextAnchor.MiddleLeft);
        title.color = Color.white;
        LayoutElement layoutElement = title.gameObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 46f;
    }

    private Text CreateText(string textValue, Transform parent, int fontSize, TextAnchor alignment)
    {
        GameObject textObject = new GameObject("Text");
        textObject.transform.SetParent(parent, false);
        Text text = textObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.text = textValue;
        return text;
    }
}
