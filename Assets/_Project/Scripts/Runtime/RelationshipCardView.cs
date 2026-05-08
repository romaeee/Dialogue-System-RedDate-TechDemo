using UnityEngine;
using UnityEngine.UI;

public sealed class RelationshipCardView : MonoBehaviour
{
    [SerializeField] private Image portraitImage;
    [SerializeField] private Text nameLabel;
    [SerializeField] private RectTransform barsRoot;

    private Font fallbackFont;

    public void Setup(
        CharacterData character,
        RelationshipTypeDatabase relationshipTypeDatabase,
        PlayerController playerController,
        RelationshipBarView relationshipBarPrefab,
        int minRelationshipValue,
        int maxRelationshipValue)
    {
        if (character == null ||
            relationshipTypeDatabase == null ||
            playerController == null ||
            relationshipBarPrefab == null ||
            barsRoot == null)
        {
            return;
        }

        if (portraitImage != null)
        {
            Sprite portrait = character.RelationshipPortrait;
            portraitImage.sprite = portrait;
            portraitImage.color = portrait != null ? Color.white : new Color(1f, 1f, 1f, 0.2f);
            portraitImage.preserveAspect = true;
        }

        if (nameLabel != null)
        {
            EnsureFont(nameLabel);
            nameLabel.text = character.CharacterName;
        }

        ClearBars();

        for (int i = 0; i < relationshipTypeDatabase.RelationshipTypes.Count; i++)
        {
            RelationshipTypeData relationshipType = relationshipTypeDatabase.RelationshipTypes[i];
            if (relationshipType == null)
            {
                continue;
            }

            RelationshipBarView bar = Instantiate(relationshipBarPrefab, barsRoot);
            int value = playerController.GetRelationshipValue(character.CharacterName, relationshipType.RelationshipName);
            bar.Setup(relationshipType.RelationshipName, value, minRelationshipValue, maxRelationshipValue);
        }
    }

    private void ClearBars()
    {
        if (barsRoot == null)
        {
            return;
        }

        for (int i = barsRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(barsRoot.GetChild(i).gameObject);
        }
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
