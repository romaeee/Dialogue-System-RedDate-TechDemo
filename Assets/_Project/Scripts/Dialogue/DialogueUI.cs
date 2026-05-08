using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
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
    private readonly ScreenTextView screenTextView;
    private readonly Text leftText;
    private readonly Text rightText;
    private readonly Text centerText;
    private readonly RectTransform choicesRoot;
    private readonly List<Button> choiceButtons = new List<Button>();

    private int selectedChoiceIndex = -1;

    public DialogueUI(Transform parent, ScreenTextView screenTextPrefab)
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
        screenTextView = CreateScreenTextView(rootObject.transform, screenTextPrefab);

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

        if (hideAfterAdvance)
        {
            yield return WaitForNextPress(wasNextPressed);
            HideAllPanels();
        }
    }

    public IEnumerator ShowScreenLine(
        string previousPageText,
        string newLineText,
        float charactersPerSecond,
        Func<bool> wasNextPressed)
    {
        HideChoices();
        screenTextView.Show();

        string separator = string.IsNullOrEmpty(previousPageText) ? string.Empty : "\n\n";
        string prefixText = previousPageText + separator;
        string fullText = prefixText + newLineText;
        screenTextView.SetText(prefixText);

        if (charactersPerSecond <= 0f)
        {
            screenTextView.SetText(fullText);
        }
        else if (screenTextView.Text != null)
        {
            yield return TypeLine(screenTextView.Text, fullText, charactersPerSecond, wasNextPressed, prefixText.Length);
        }
        else
        {
            screenTextView.SetText(fullText);
        }

        yield return WaitForNextPress(wasNextPressed);
    }

    public void ClearScreenTextPage()
    {
        screenTextView.SetText(string.Empty);
        screenTextView.Hide();
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

    public void ShowStaticLine(DialogueLine line)
    {
        if (line == null)
        {
            return;
        }

        Text activeText = GetTextForSpeaker(line.SpeakerName);
        ShowOnlyPanel(activeText);
        activeText.text = $"{line.SpeakerName}: {line.Text}";
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

    public void Hide()
    {
        HideAll();
    }

    private static IEnumerator TypeLine(
        Text targetText,
        string fullText,
        float charactersPerSecond,
        Func<bool> wasNextPressed,
        int startVisibleCharacters = 0)
    {
        float secondsPerCharacter = 1f / charactersPerSecond;
        float timer = 0f;
        int visibleCharacters = Mathf.Clamp(startVisibleCharacters, 0, fullText.Length);
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

    private static IEnumerator TypeLine(
        TMP_Text targetText,
        string fullText,
        float charactersPerSecond,
        Func<bool> wasNextPressed,
        int startVisibleCharacters = 0)
    {
        float secondsPerCharacter = 1f / charactersPerSecond;
        float timer = 0f;
        int visibleCharacters = Mathf.Clamp(startVisibleCharacters, 0, fullText.Length);
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
        screenTextView.Hide();
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
        screenTextView.Hide();
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

    private static ScreenTextView CreateScreenTextView(Transform parent, ScreenTextView prefab)
    {
        ScreenTextView view = prefab != null ?
            UnityEngine.Object.Instantiate(prefab, parent, false) :
            CreateGeneratedScreenTextView(parent);

        view.name = "Screen Text";
        view.AutoBind();

        if (view.Text == null)
        {
            Debug.LogError("Screen Text prefab needs a TMP_Text component assigned on ScreenTextView.");
        }

        return view;
    }

    private static ScreenTextView CreateGeneratedScreenTextView(Transform parent)
    {
        GameObject panel = new GameObject("Screen Text");
        panel.transform.SetParent(parent, false);

        RectTransform panelTransform = panel.AddComponent<RectTransform>();
        panelTransform.anchorMin = Vector2.zero;
        panelTransform.anchorMax = Vector2.one;
        panelTransform.offsetMin = Vector2.zero;
        panelTransform.offsetMax = Vector2.zero;

        GameObject textObject = new GameObject("Text");
        textObject.transform.SetParent(panel.transform, false);
        TMP_Text text = textObject.AddComponent<TextMeshProUGUI>();
        ConfigureScreenText(text);
        RectTransform textTransform = text.GetComponent<RectTransform>();
        textTransform.anchorMin = new Vector2(0f, 0.55f);
        textTransform.anchorMax = new Vector2(1f, 1f);
        textTransform.offsetMin = new Vector2(68f, -4f);
        textTransform.offsetMax = new Vector2(-76f, -54f);

        return panel.AddComponent<ScreenTextView>();
    }

    private static void ConfigureScreenText(TMP_Text text)
    {
        text.fontSize = 54f;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Truncate;
        text.lineSpacing = 1.05f;
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
