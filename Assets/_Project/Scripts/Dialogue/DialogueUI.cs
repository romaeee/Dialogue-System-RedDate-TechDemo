using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public sealed class DialogueUI
{
    private readonly GameObject rootObject;
    private readonly GameObject leftPanel;
    private readonly GameObject rightPanel;
    private readonly GameObject centerPanel;
    private readonly Text leftText;
    private readonly Text rightText;
    private readonly Text centerText;
    private readonly RectTransform choicesRoot;
    private readonly List<Button> choiceButtons = new List<Button>();

    private int selectedChoiceIndex = -1;

    public DialogueUI(Transform parent)
    {
        EnsureEventSystem();

        rootObject = new GameObject("Dialogue UI");
        rootObject.transform.SetParent(parent, false);

        Canvas canvas = rootObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler canvasScaler = rootObject.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasScaler.matchWidthOrHeight = 0.5f;

        rootObject.AddComponent<GraphicRaycaster>();

        Font font = GetBuiltinFont();
        leftPanel = CreateDialoguePanel("Character Phrase", rootObject.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(48f, 0f), out leftText, font);
        rightPanel = CreateDialoguePanel("Player Phrase", rootObject.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-48f, 0f), out rightText, font);
        centerPanel = CreateDialoguePanel("Narrator Phrase", rootObject.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 72f), out centerText, font);

        GameObject choicesObject = new GameObject("Choices");
        choicesObject.transform.SetParent(rootObject.transform, false);
        choicesRoot = choicesObject.AddComponent<RectTransform>();
        choicesRoot.anchorMin = new Vector2(1f, 0.35f);
        choicesRoot.anchorMax = new Vector2(1f, 0.35f);
        choicesRoot.pivot = new Vector2(1f, 0.5f);
        choicesRoot.anchoredPosition = new Vector2(-48f, 0f);
        choicesRoot.sizeDelta = new Vector2(430f, 460f);

        VerticalLayoutGroup layoutGroup = choicesObject.AddComponent<VerticalLayoutGroup>();
        layoutGroup.spacing = 18f;
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = false;

        HideAll();
    }

    public int SelectedChoiceIndex => selectedChoiceIndex;

    public IEnumerator ShowLine(
        DialogueLine line,
        float charactersPerSecond,
        Func<bool> wasNextPressed,
        bool hideAfterAdvance = true)
    {
        HideChoices();

        Text activeText = GetTextForSpeaker(line.SpeakerName);
        ShowOnlyPanel(activeText);

        string fullText = $"{line.SpeakerName}: {line.Text}";
        activeText.text = string.Empty;

        if (charactersPerSecond <= 0f)
        {
            activeText.text = fullText;
        }
        else
        {
            yield return TypeLine(activeText, fullText, charactersPerSecond, wasNextPressed);
        }

        yield return WaitForNextPress(wasNextPressed);

        if (hideAfterAdvance)
        {
            HideAllPanels();
        }
    }

    public void ShowChoices(IReadOnlyList<DialogueChoice> choices)
    {
        selectedChoiceIndex = -1;
        ClearChoices();
        choicesRoot.gameObject.SetActive(true);

        for (int i = 0; i < choices.Count; i++)
        {
            int choiceIndex = i;
            Button button = CreateChoiceButton(choices[i].BoxText);
            button.onClick.AddListener(() => SelectChoice(choiceIndex));
            choiceButtons.Add(button);
        }
    }

    public void SelectChoice(int choiceIndex)
    {
        selectedChoiceIndex = choiceIndex;
    }

    public void HideChoices()
    {
        choicesRoot.gameObject.SetActive(false);
        ClearChoices();
        HideAllPanels();
    }

    private static IEnumerator TypeLine(Text targetText, string fullText, float charactersPerSecond, Func<bool> wasNextPressed)
    {
        float secondsPerCharacter = 1f / charactersPerSecond;
        float timer = 0f;
        int visibleCharacters = 0;
        int lastVisibleCharacters = -1;

        while (visibleCharacters < fullText.Length)
        {
            if (wasNextPressed())
            {
                targetText.text = fullText;
                yield return null;
                yield break;
            }

            timer += Time.deltaTime;
            while (timer >= secondsPerCharacter && visibleCharacters < fullText.Length)
            {
                timer -= secondsPerCharacter;
                visibleCharacters++;
            }

            if (visibleCharacters != lastVisibleCharacters)
            {
                targetText.text = fullText.Substring(0, visibleCharacters);
                lastVisibleCharacters = visibleCharacters;
            }

            yield return null;
        }
    }

    private static IEnumerator WaitForNextPress(Func<bool> wasNextPressed)
    {
        while (!wasNextPressed())
        {
            yield return null;
        }

        yield return null;
    }

    private Text GetTextForSpeaker(string speakerName)
    {
        if (speakerName == "Player")
        {
            return rightText;
        }

        return leftText;
    }

    private void ShowOnlyPanel(Text activeText)
    {
        leftPanel.SetActive(activeText == leftText);
        rightPanel.SetActive(activeText == rightText);
        centerPanel.SetActive(activeText == centerText);
    }

    private void HideAll()
    {
        HideAllPanels();
        HideChoices();
    }

    private void HideAllPanels()
    {
        leftPanel.SetActive(false);
        rightPanel.SetActive(false);
        centerPanel.SetActive(false);
    }

    private void ClearChoices()
    {
        for (int i = 0; i < choiceButtons.Count; i++)
        {
            if (choiceButtons[i] != null)
            {
                UnityEngine.Object.Destroy(choiceButtons[i].gameObject);
            }
        }

        choiceButtons.Clear();
    }

    private Button CreateChoiceButton(string label)
    {
        Font font = GetBuiltinFont();

        GameObject buttonObject = new GameObject("Choice");
        buttonObject.transform.SetParent(choicesRoot, false);

        Image background = buttonObject.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 1f);

        Button button = buttonObject.AddComponent<Button>();

        LayoutElement layoutElement = buttonObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 92f;

        GameObject textObject = new GameObject("Text");
        textObject.transform.SetParent(buttonObject.transform, false);
        Text text = textObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = 30;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.text = label;

        RectTransform textTransform = text.GetComponent<RectTransform>();
        textTransform.anchorMin = Vector2.zero;
        textTransform.anchorMax = Vector2.one;
        textTransform.offsetMin = new Vector2(18f, 10f);
        textTransform.offsetMax = new Vector2(-18f, -10f);

        return button;
    }

    private static GameObject CreateDialoguePanel(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        out Text text,
        Font font)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);

        RectTransform panelTransform = panel.AddComponent<RectTransform>();
        panelTransform.anchorMin = anchorMin;
        panelTransform.anchorMax = anchorMax;
        panelTransform.pivot = pivot;
        panelTransform.anchoredPosition = anchoredPosition;
        panelTransform.sizeDelta = new Vector2(560f, 180f);

        Image image = panel.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 1f);

        GameObject textObject = new GameObject("Text");
        textObject.transform.SetParent(panel.transform, false);
        text = textObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = 34;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleLeft;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;

        RectTransform textTransform = text.GetComponent<RectTransform>();
        textTransform.anchorMin = Vector2.zero;
        textTransform.anchorMax = Vector2.one;
        textTransform.offsetMin = new Vector2(28f, 20f);
        textTransform.offsetMax = new Vector2(-28f, -20f);

        return panel;
    }

    private static Font GetBuiltinFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        return font;
    }

    private static void EnsureEventSystem()
    {
        if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        InputSystemUIInputModule inputModule = eventSystemObject.AddComponent<InputSystemUIInputModule>();
        inputModule.AssignDefaultActions();
    }
}
