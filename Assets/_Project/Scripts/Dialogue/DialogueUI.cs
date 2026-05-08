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
    private readonly DialogueBoxView leftBox;
    private readonly DialogueBoxView rightBox;
    private readonly DialogueBoxView centerBox;
    private readonly ScreenTextView screenTextView;
    private readonly RectTransform choicesRoot;
    private readonly List<Button> choiceButtons = new List<Button>();

    private int selectedChoiceIndex = -1;

    public DialogueUI(
        Transform parent,
        ScreenTextView screenTextPrefab,
        DialogueBoxView characterDialogueBoxPrefab,
        DialogueBoxView playerDialogueBoxPrefab,
        DialogueBoxView narratorDialogueBoxPrefab)
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

        leftBox = CreateDialogueBox(
            "Character Phrase",
            rootObject.transform,
            characterDialogueBoxPrefab,
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(48f, 0f));
        rightBox = CreateDialogueBox(
            "Player Phrase",
            rootObject.transform,
            playerDialogueBoxPrefab,
            new Vector2(1f, 0.5f),
            new Vector2(1f, 0.5f),
            new Vector2(1f, 0.5f),
            new Vector2(-48f, 0f));
        centerBox = CreateDialogueBox(
            "Narrator Phrase",
            rootObject.transform,
            narratorDialogueBoxPrefab,
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(48f, 0f));
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

        DialogueBoxView activeBox = GetBoxForSpeaker(line.SpeakerName);
        ShowOnlyBox(activeBox);

        string fullText = $"{line.SpeakerName}: {line.Text}";
        activeBox.SetText(string.Empty);

        if (charactersPerSecond <= 0f)
        {
            activeBox.SetText(fullText);
        }
        else if (activeBox.Text != null)
        {
            yield return TypeLine(activeBox.Text, fullText, charactersPerSecond, wasNextPressed);
        }
        else
        {
            activeBox.SetText(fullText);
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

        DialogueBoxView activeBox = GetBoxForSpeaker(line.SpeakerName);
        ShowOnlyBox(activeBox);
        activeBox.SetText($"{line.SpeakerName}: {line.Text}");
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

    private DialogueBoxView GetBoxForSpeaker(string speakerName)
    {
        if (speakerName == "Player")
        {
            return rightBox;
        }

        if (speakerName == "Narrator")
        {
            return centerBox;
        }

        return leftBox;
    }

    private void ShowOnlyBox(DialogueBoxView activeBox)
    {
        leftBox.gameObject.SetActive(activeBox == leftBox);
        rightBox.gameObject.SetActive(activeBox == rightBox);
        centerBox.gameObject.SetActive(activeBox == centerBox);
        screenTextView.Hide();
    }

    private void HideAll()
    {
        HideAllPanels();
        HideChoices();
    }

    private void HideAllPanels()
    {
        leftBox.Hide();
        rightBox.Hide();
        centerBox.Hide();
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

    private static DialogueBoxView CreateDialogueBox(
        string name,
        Transform parent,
        DialogueBoxView prefab,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition)
    {
        DialogueBoxView view = prefab != null ?
            UnityEngine.Object.Instantiate(prefab, parent, false) :
            CreateGeneratedDialogueBox(parent);

        view.name = name;
        view.AutoBind();
        ConfigureDialogueBoxTransform(view.GetComponent<RectTransform>(), anchorMin, anchorMax, pivot, anchoredPosition);

        if (view.Text == null)
        {
            Debug.LogError($"Dialogue box prefab \"{name}\" needs a TMP_Text component assigned on DialogueBoxView.");
        }

        return view;
    }

    private static DialogueBoxView CreateGeneratedDialogueBox(Transform parent)
    {
        GameObject panel = new GameObject("Dialogue Box");
        panel.transform.SetParent(parent, false);

        RectTransform panelTransform = panel.AddComponent<RectTransform>();
        panelTransform.sizeDelta = new Vector2(560f, 180f);

        Image image = panel.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 1f);

        GameObject textObject = new GameObject("Text");
        textObject.transform.SetParent(panel.transform, false);
        TMP_Text text = textObject.AddComponent<TextMeshProUGUI>();
        ConfigureDialogueBoxText(text);

        RectTransform textTransform = text.GetComponent<RectTransform>();
        textTransform.anchorMin = Vector2.zero;
        textTransform.anchorMax = Vector2.one;
        textTransform.offsetMin = new Vector2(28f, 20f);
        textTransform.offsetMax = new Vector2(-28f, -20f);

        return panel.AddComponent<DialogueBoxView>();
    }

    private static void ConfigureDialogueBoxTransform(
        RectTransform rectTransform,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition)
    {
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = pivot;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = new Vector2(560f, 180f);
    }

    private static void ConfigureDialogueBoxText(TMP_Text text)
    {
        text.fontSize = 34f;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Truncate;
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
