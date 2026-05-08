using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class DialogueRunner : MonoBehaviour, ISavable<DialogueSaveData>
{
    [SerializeField] private TextAsset dialogueText;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private CharacterDatabase characterDatabase;
    [SerializeField] private BackgroundDatabase backgroundDatabase;
    [SerializeField] private string saveFileName = "save.json";
    [SerializeField] private Button saveButton;
    [SerializeField] private Button loadButton;
    [SerializeField] private Button restoreButton;
    [SerializeField] private Button logButton;
    [SerializeField] private GameObject logPanelRoot;
    [SerializeField] private TMP_Text logText;
    [SerializeField] private ScrollRect logScrollRect;
    [SerializeField] private DialogueDisplayMode displayMode = DialogueDisplayMode.TextBoxes;
    [SerializeField, Min(1)] private int screenTextLinesPerPage = 6;
    [SerializeField] private ScreenTextView screenTextPrefab;
    [SerializeField] private DialogueBoxView characterDialogueBoxPrefab;
    [SerializeField] private DialogueBoxView playerDialogueBoxPrefab;
    [SerializeField] private DialogueBoxView narratorDialogueBoxPrefab;
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool verboseLogging;
    [SerializeField] private float lineDelaySeconds = 1f;
    [SerializeField] private float typewriterCharactersPerSecond = 45f;
    [SerializeField] private float characterScreenHeight = 0.75f;

    private Coroutine playRoutine;
    private Camera targetCamera;
    private SpriteRenderer backgroundRenderer;
    private DialogueUI dialogueUI;
    private WaitForSeconds cachedLineDelay;
    private float cachedLineDelaySeconds = -1f;
    private DialogueGraph currentGraph;
    private string currentNodeName = "Start";
    private string currentHubName;
    private int currentElementIndex;
    private DialogueLine retainedLineBeforeChoices;
    private string currentBackgroundName;
    private readonly Dictionary<string, DialogueHub> hubsByName = new Dictionary<string, DialogueHub>();
    private readonly Dictionary<string, DialogueNode> nodesByName = new Dictionary<string, DialogueNode>();
    private readonly HashSet<int> usedOnceChoiceLineNumbers = new HashSet<int>();
    private readonly HashSet<string> visibleCharacterNames = new HashSet<string>();
    private readonly Dictionary<string, SpriteRenderer> characterRenderers = new Dictionary<string, SpriteRenderer>();
    private readonly Dictionary<string, CharacterEmotion> currentCharacterEmotions = new Dictionary<string, CharacterEmotion>();
    private readonly List<string> logEntries = new List<string>();
    private readonly List<string> screenTextPageLines = new List<string>();
    private readonly StringBuilder logBuilder = new StringBuilder();
    private bool suppressNextLineLog;

    private void Start()
    {
        BindSceneButtons();

        if (playOnStart)
        {
            Play();
        }
    }

    private void OnDestroy()
    {
        if (saveButton != null)
        {
            saveButton.onClick.RemoveListener(OnSaveButtonClicked);
        }

        if (loadButton != null)
        {
            loadButton.onClick.RemoveListener(OnLoadButtonClicked);
        }

        if (restoreButton != null)
        {
            restoreButton.onClick.RemoveListener(OnRestoreButtonClicked);
        }

        if (logButton != null)
        {
            logButton.onClick.RemoveListener(ToggleLogPanel);
        }
    }

    public void Play()
    {
        if (dialogueText == null)
        {
            Debug.LogError("DialogueRunner needs a dialogue text asset.");
            return;
        }

        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
        }

        DialogueGraph graph = BuildGraph();
        ResetRuntimeState();
        ResetPlayerState();
        StartGraph(graph);
    }

    public DialogueSaveData CaptureState()
    {
        DialogueLine retainedLine = GetRetainedLineForSave();
        DialogueSaveData state = new DialogueSaveData
        {
            nodeName = currentNodeName,
            elementIndex = currentElementIndex,
            activeHubName = currentHubName,
            hasRetainedLine = retainedLine != null,
            retainedLineSpeakerName = retainedLine != null ? retainedLine.SpeakerName : null,
            retainedLineText = retainedLine != null ? retainedLine.Text : null,
            currentBackgroundName = currentBackgroundName,
            visibleCharacterNames = new List<string>(visibleCharacterNames),
            characterEmotions = CaptureCharacterEmotionState(),
            usedOnceChoiceLineNumbers = new List<int>(usedOnceChoiceLineNumbers),
            logEntries = new List<string>(logEntries)
        };

        return state;
    }

    public void RestoreState(DialogueSaveData state)
    {
        if (state == null)
        {
            Debug.LogWarning("Cannot restore null dialogue save data.");
            return;
        }

        DialogueGraph graph = BuildGraph();
        ResetRuntimeState();

        logEntries.Clear();
        if (state.logEntries != null)
        {
            logEntries.AddRange(state.logEntries);
        }

        RefreshLogPanel();

        for (int i = 0; i < state.usedOnceChoiceLineNumbers.Count; i++)
        {
            usedOnceChoiceLineNumbers.Add(state.usedOnceChoiceLineNumbers[i]);
        }

        if (!string.IsNullOrWhiteSpace(state.currentBackgroundName))
        {
            ShowBackground(state.currentBackgroundName);
        }

        for (int i = 0; i < state.visibleCharacterNames.Count; i++)
        {
            ShowCharacter(state.visibleCharacterNames[i]);
        }

        if (state.characterEmotions != null)
        {
            for (int i = 0; i < state.characterEmotions.Count; i++)
            {
                CharacterEmotionSaveData emotionState = state.characterEmotions[i];
                if (emotionState != null &&
                    Enum.TryParse(emotionState.emotionName, true, out CharacterEmotion emotion))
                {
                    ApplyCharacterEmotion(emotionState.characterName, emotion);
                }
            }
        }

        if (!nodesByName.TryGetValue(state.nodeName, out DialogueNode node))
        {
            Debug.LogWarning($"Saved dialogue node \"{state.nodeName}\" was not found. Loading from start.");
            node = graph.StartNode;
        }

        int startIndex = Mathf.Clamp(state.elementIndex, 0, Mathf.Max(0, node.Elements.Count - 1));
        currentNodeName = node.NodeName;
        currentElementIndex = startIndex;

        EnsureDialogueUI();

        if (state.hasRetainedLine)
        {
            retainedLineBeforeChoices = new DialogueLine(
                0,
                state.retainedLineSpeakerName,
                state.retainedLineText);
            dialogueUI.ShowStaticLine(retainedLineBeforeChoices);
        }
        else if (!string.IsNullOrWhiteSpace(state.activeHubName) && TryGetLineBeforeElement(node, startIndex, out DialogueLine lineBeforeHub))
        {
            retainedLineBeforeChoices = lineBeforeHub;
            dialogueUI.ShowStaticLine(retainedLineBeforeChoices);
        }
        else if (startIndex < node.Elements.Count && node.Elements[startIndex] is DialogueLine)
        {
            suppressNextLineLog = true;
        }

        playRoutine = StartCoroutine(PlayRestoredPosition(node, startIndex, state.activeHubName));
    }

    private async System.Threading.Tasks.Task SaveAsync()
    {
        SaveGameData saveData = new SaveGameData
        {
            dialogue = CaptureState(),
            player = playerController != null ? playerController.CaptureState() : new PlayerSaveData()
        };

        await SaveSystem.SaveAsync(saveFileName, saveData);
    }

    private async void OnSaveButtonClicked()
    {
        await SaveAsync();
    }

    private async void OnLoadButtonClicked()
    {
        await LoadAsync();
    }

    private async void OnRestoreButtonClicked()
    {
        await RestoreSaveAsync();
    }

    private void EnsureDialogueUI()
    {
        if (dialogueUI != null)
        {
            return;
        }

        dialogueUI = new DialogueUI(
            transform,
            screenTextPrefab,
            characterDialogueBoxPrefab,
            playerDialogueBoxPrefab,
            narratorDialogueBoxPrefab);
    }

    private void BindSceneButtons()
    {
        if (saveButton != null)
        {
            saveButton.onClick.RemoveListener(OnSaveButtonClicked);
            saveButton.onClick.AddListener(OnSaveButtonClicked);
        }
        else
        {
            Debug.LogWarning("DialogueRunner save button is not assigned.");
        }

        if (loadButton != null)
        {
            loadButton.onClick.RemoveListener(OnLoadButtonClicked);
            loadButton.onClick.AddListener(OnLoadButtonClicked);
        }
        else
        {
            Debug.LogWarning("DialogueRunner load button is not assigned.");
        }

        if (restoreButton != null)
        {
            restoreButton.onClick.RemoveListener(OnRestoreButtonClicked);
            restoreButton.onClick.AddListener(OnRestoreButtonClicked);
        }
        else
        {
            Debug.LogWarning("DialogueRunner restore button is not assigned.");
        }

        if (logButton != null)
        {
            logButton.onClick.RemoveListener(ToggleLogPanel);
            logButton.onClick.AddListener(ToggleLogPanel);
        }
        else
        {
            Debug.LogWarning("DialogueRunner log button is not assigned.");
        }

        if (logPanelRoot != null)
        {
            logPanelRoot.SetActive(false);
        }
    }

    private async System.Threading.Tasks.Task LoadAsync()
    {
        SaveGameData saveData = await SaveSystem.LoadAsync(saveFileName);
        if (saveData == null)
        {
            return;
        }

        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }

        if (playerController != null)
        {
            playerController.ResetState();
            playerController.RestoreState(saveData.player);
        }

        RestoreState(saveData.dialogue);
    }

    private async System.Threading.Tasks.Task RestoreSaveAsync()
    {
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }

        await SaveSystem.DeleteAsync(saveFileName);
        Play();
    }

    private DialogueGraph BuildGraph()
    {
        currentGraph = DialogueParser.Parse(dialogueText);
        BuildGraphLookup(currentGraph);
        return currentGraph;
    }

    private void StartGraph(DialogueGraph graph)
    {
        usedOnceChoiceLineNumbers.Clear();
        currentNodeName = graph.StartNode != null ? graph.StartNode.NodeName : "Start";
        currentElementIndex = 0;

        EnsureDialogueUI();

        playRoutine = StartCoroutine(PlayGraph(graph));
    }

    private void ResetRuntimeState()
    {
        currentBackgroundName = null;
        currentHubName = null;
        retainedLineBeforeChoices = null;
        suppressNextLineLog = false;
        visibleCharacterNames.Clear();
        currentCharacterEmotions.Clear();
        usedOnceChoiceLineNumbers.Clear();
        screenTextPageLines.Clear();
        logEntries.Clear();
        RefreshLogPanel();

        if (backgroundRenderer != null)
        {
            backgroundRenderer.enabled = false;
        }

        foreach (SpriteRenderer renderer in characterRenderers.Values)
        {
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }

        if (dialogueUI != null)
        {
            dialogueUI.Hide();
        }
    }

    private void ResetPlayerState()
    {
        if (playerController != null)
        {
            playerController.ResetState();
        }
    }

    private IEnumerator PlayGraph(DialogueGraph graph)
    {
        if (graph.StartNode == null)
        {
            yield break;
        }

        LogVerbose("[Dialogue] Start");
        yield return PlayNode(graph.StartNode, 0, false);
        LogVerbose("[Dialogue] End");
        playRoutine = null;
    }

    private IEnumerator PlayNode(DialogueNode node, int startIndex, bool stopAfterHub)
    {
        currentNodeName = node.NodeName;

        for (int i = startIndex; i < node.Elements.Count; i++)
        {
            currentNodeName = node.NodeName;
            currentElementIndex = i;
            currentHubName = null;
            DialogueElement element = node.Elements[i];

            if (element is DialogueLine line)
            {
                if (!AreInventoryConditionsMet(line.InventoryConditions) ||
                    !AreRelationshipConditionsMet(line.RelationshipConditions) ||
                    !AreVariableConditionsMet(line.VariableConditions))
                {
                    retainedLineBeforeChoices = null;
                    continue;
                }

                LogVerbose($"{line.SpeakerName}: {line.Text}");
                AddLogEntry(line);
                ApplyRelationshipChanges(line.RelationshipChanges);
                ApplyInventoryChanges(line.InventoryChanges);
                ApplyVariableChanges(line.VariableChanges);
                ApplyEmotionChanges(line.EmotionChanges);
                bool hideAfterAdvance = !(i + 1 < node.Elements.Count && node.Elements[i + 1] is DialogueHub);
                yield return ShowDialogueLine(line, hideAfterAdvance);
                retainedLineBeforeChoices = hideAfterAdvance ? null : line;
                continue;
            }

            if (element is DialogueCommand command)
            {
                HandleCommand(command);
                retainedLineBeforeChoices = null;
                if (command.CommandType != DialogueCommandType.StartNextPage)
                {
                    yield return WaitForLineDelay();
                }

                continue;
            }

            if (element is DialogueHub hub)
            {
                if (displayMode == DialogueDisplayMode.ScreenText)
                {
                    retainedLineBeforeChoices = null;
                    continue;
                }

                yield return PlayHub(hub);

                if (stopAfterHub)
                {
                    yield break;
                }
            }
        }
    }

    private IEnumerator PlayHub(DialogueHub hub)
    {
        currentHubName = hub.HubName;
        LogVerbose($"[Hub] {hub.HubName}");
        List<DialogueChoice> availableChoices = GetAvailableChoices(hub);

        for (int i = 0; i < availableChoices.Count; i++)
        {
            DialogueChoice choice = availableChoices[i];
            LogVerbose($"[Choice {i + 1}] {choice.BoxText}");
        }

        if (availableChoices.Count == 0)
        {
            yield break;
        }

        dialogueUI.ShowChoices(availableChoices);

        while (dialogueUI.SelectedChoiceIndex < 0)
        {
            int keyboardChoiceIndex = GetPressedChoiceIndex(availableChoices.Count);
            if (keyboardChoiceIndex >= 0)
            {
                dialogueUI.SelectChoice(keyboardChoiceIndex);
            }

            yield return null;
        }

        DialogueChoice selectedChoice = availableChoices[dialogueUI.SelectedChoiceIndex];
        if (selectedChoice.IsOnce)
        {
            usedOnceChoiceLineNumbers.Add(selectedChoice.LineNumber);
        }

        ApplyRelationshipChanges(selectedChoice.RelationshipChanges);
        ApplyInventoryChanges(selectedChoice.InventoryChanges);
        ApplyVariableChanges(selectedChoice.VariableChanges);
        ApplyEmotionChanges(selectedChoice.EmotionChanges);
        dialogueUI.HideChoices();
        retainedLineBeforeChoices = null;
        currentHubName = null;

        if (selectedChoice.HasHubTarget)
        {
            if (hubsByName.TryGetValue(selectedChoice.TargetHubName, out DialogueHub targetHub))
            {
                yield return PlayHub(targetHub);
            }
            else
            {
                Debug.LogError($"Hub \"{selectedChoice.TargetHubName}\" was not found.");
            }

            yield break;
        }

        LogVerbose($"{selectedChoice.SpeakerName}: {selectedChoice.SelectedText}");
        DialogueLine selectedLine = new DialogueLine(selectedChoice.LineNumber, selectedChoice.SpeakerName, selectedChoice.SelectedText);
        AddLogEntry(selectedLine);
        yield return ShowDialogueLine(selectedLine, true);
        retainedLineBeforeChoices = null;
        yield return PlayNode(selectedChoice.ConsequenceNode, 0, true);
    }

    private IEnumerator ShowDialogueLine(DialogueLine line, bool hideAfterAdvance)
    {
        if (displayMode == DialogueDisplayMode.ScreenText)
        {
            int maxLinesPerPage = Mathf.Max(1, screenTextLinesPerPage);
            if (screenTextPageLines.Count >= maxLinesPerPage)
            {
                StartNextScreenTextPage();
            }

            string previousPageText = BuildScreenTextPage();
            string newLineText = $"\"{line.Text}\"";
            yield return dialogueUI.ShowScreenLine(previousPageText, newLineText, typewriterCharactersPerSecond, WasNextPressed);
            screenTextPageLines.Add(newLineText);
            yield break;
        }

        yield return dialogueUI.ShowLine(line, typewriterCharactersPerSecond, WasNextPressed, hideAfterAdvance);
    }

    private void ToggleLogPanel()
    {
        if (logPanelRoot == null)
        {
            Debug.LogWarning("DialogueRunner log panel root is not assigned.");
            return;
        }

        bool shouldShow = !logPanelRoot.activeSelf;
        logPanelRoot.SetActive(shouldShow);

        if (shouldShow)
        {
            RefreshLogPanel();
        }
    }

    private void AddLogEntry(DialogueLine line)
    {
        if (line == null)
        {
            return;
        }

        if (suppressNextLineLog)
        {
            suppressNextLineLog = false;
            return;
        }

        logEntries.Add($"{line.SpeakerName}: {line.Text}");
        RefreshLogPanel();
    }

    private DialogueLine GetRetainedLineForSave()
    {
        if (retainedLineBeforeChoices != null)
        {
            return retainedLineBeforeChoices;
        }

        if (string.IsNullOrWhiteSpace(currentHubName) ||
            !nodesByName.TryGetValue(currentNodeName, out DialogueNode node) ||
            !TryGetLineBeforeElement(node, currentElementIndex, out DialogueLine lineBeforeHub))
        {
            return null;
        }

        return lineBeforeHub;
    }

    private static bool TryGetLineBeforeElement(DialogueNode node, int elementIndex, out DialogueLine line)
    {
        line = null;

        if (node == null)
        {
            return false;
        }

        for (int i = Mathf.Min(elementIndex - 1, node.Elements.Count - 1); i >= 0; i--)
        {
            if (node.Elements[i] is DialogueLine previousLine)
            {
                line = previousLine;
                return true;
            }

            if (node.Elements[i] is DialogueHub || node.Elements[i] is DialogueCommand)
            {
                return false;
            }
        }

        return false;
    }

    private void AddLogEntry(string entry)
    {
        if (string.IsNullOrWhiteSpace(entry))
        {
            return;
        }

        logEntries.Add(entry);
        RefreshLogPanel();
    }

    private void RefreshLogPanel()
    {
        if (logText == null)
        {
            return;
        }

        logBuilder.Length = 0;
        for (int i = 0; i < logEntries.Count; i++)
        {
            if (i > 0)
            {
                logBuilder.AppendLine();
                logBuilder.AppendLine();
            }

            logBuilder.Append(logEntries[i]);
        }

        logText.text = logBuilder.ToString();

        if (logScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            logScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    private IEnumerator PlayRestoredPosition(DialogueNode node, int startIndex, string activeHubName)
    {
        if (!string.IsNullOrWhiteSpace(activeHubName) && hubsByName.TryGetValue(activeHubName, out DialogueHub restoredHub))
        {
            yield return PlayHub(restoredHub);
            yield break;
        }

        yield return PlayNode(node, startIndex, false);
    }

    private void ApplyRelationshipChanges(IReadOnlyList<RelationshipChange> relationshipChanges)
    {
        if (relationshipChanges == null || relationshipChanges.Count == 0)
        {
            return;
        }

        if (playerController == null)
        {
            Debug.LogWarning("DialogueRunner has relationship changes, but PlayerController is not assigned.");
            return;
        }

        for (int i = 0; i < relationshipChanges.Count; i++)
        {
            playerController.ApplyRelationshipChange(relationshipChanges[i]);
        }
    }

    private void ApplyInventoryChanges(IReadOnlyList<InventoryChange> inventoryChanges)
    {
        if (inventoryChanges == null || inventoryChanges.Count == 0)
        {
            return;
        }

        if (playerController == null)
        {
            Debug.LogWarning("DialogueRunner has inventory changes, but PlayerController is not assigned.");
            return;
        }

        for (int i = 0; i < inventoryChanges.Count; i++)
        {
            InventoryChange inventoryChange = inventoryChanges[i];
            playerController.ApplyInventoryChange(inventoryChange);
            AddLogEntry(inventoryChange.ShouldAdd ? $"Got item: {inventoryChange.ItemName}" : $"Lost item: {inventoryChange.ItemName}");
        }
    }

    private List<DialogueChoice> GetAvailableChoices(DialogueHub hub)
    {
        List<DialogueChoice> availableChoices = new List<DialogueChoice>();

        for (int i = 0; i < hub.Choices.Count; i++)
        {
            DialogueChoice choice = hub.Choices[i];
            if (choice.IsOnce && usedOnceChoiceLineNumbers.Contains(choice.LineNumber))
            {
                continue;
            }

            if (!AreInventoryConditionsMet(choice.InventoryConditions))
            {
                continue;
            }

            if (!AreRelationshipConditionsMet(choice.RelationshipConditions))
            {
                continue;
            }

            if (!AreVariableConditionsMet(choice.VariableConditions))
            {
                continue;
            }

            availableChoices.Add(choice);
        }

        return availableChoices;
    }

    private bool AreRelationshipConditionsMet(IReadOnlyList<RelationshipCondition> relationshipConditions)
    {
        if (relationshipConditions == null || relationshipConditions.Count == 0)
        {
            return true;
        }

        if (playerController == null)
        {
            Debug.LogWarning("DialogueRunner has relationship conditions, but PlayerController is not assigned.");
            return false;
        }

        for (int i = 0; i < relationshipConditions.Count; i++)
        {
            RelationshipCondition condition = relationshipConditions[i];
            int currentValue = playerController.GetRelationshipValue(condition.CharacterName, condition.RelationshipTypeName);
            if (!CompareRelationshipValue(currentValue, condition.ComparisonOperator, condition.Value))
            {
                return false;
            }
        }

        return true;
    }

    private static bool CompareRelationshipValue(int currentValue, string comparisonOperator, int targetValue)
    {
        switch (comparisonOperator)
        {
            case ">":
                return currentValue > targetValue;
            case "<":
                return currentValue < targetValue;
            case ">=":
                return currentValue >= targetValue;
            case "<=":
                return currentValue <= targetValue;
            case "==":
                return currentValue == targetValue;
            case "!=":
                return currentValue != targetValue;
            default:
                return false;
        }
    }

    private bool AreInventoryConditionsMet(IReadOnlyList<InventoryCondition> inventoryConditions)
    {
        if (inventoryConditions == null || inventoryConditions.Count == 0)
        {
            return true;
        }

        if (playerController == null)
        {
            Debug.LogWarning("DialogueRunner has inventory conditions, but PlayerController is not assigned.");
            return false;
        }

        for (int i = 0; i < inventoryConditions.Count; i++)
        {
            InventoryCondition condition = inventoryConditions[i];
            bool hasItem = playerController.HasItem(condition.ItemName);
            if (hasItem != condition.ShouldHaveItem)
            {
                return false;
            }
        }

        return true;
    }

    private void ApplyVariableChanges(IReadOnlyList<VariableChange> variableChanges)
    {
        if (variableChanges == null || variableChanges.Count == 0)
        {
            return;
        }

        if (playerController == null)
        {
            Debug.LogWarning("DialogueRunner has variable changes, but PlayerController is not assigned.");
            return;
        }

        for (int i = 0; i < variableChanges.Count; i++)
        {
            playerController.SetVariable(variableChanges[i]);
        }
    }

    private void ApplyEmotionChanges(IReadOnlyList<CharacterEmotionChange> emotionChanges)
    {
        if (emotionChanges == null || emotionChanges.Count == 0)
        {
            return;
        }

        for (int i = 0; i < emotionChanges.Count; i++)
        {
            CharacterEmotionChange emotionChange = emotionChanges[i];
            if (emotionChange != null)
            {
                ApplyCharacterEmotion(emotionChange.CharacterName, emotionChange.Emotion);
            }
        }
    }

    private void ApplyCharacterEmotion(string characterName, CharacterEmotion emotion)
    {
        if (string.IsNullOrWhiteSpace(characterName))
        {
            return;
        }

        if (!visibleCharacterNames.Contains(characterName))
        {
            Debug.LogWarning($"Cannot switch \"{characterName}\" to {emotion}: character is not visible on stage.");
            return;
        }

        if (characterDatabase == null)
        {
            Debug.LogError("DialogueRunner needs a character database.");
            return;
        }

        CharacterData character = characterDatabase.GetByName(characterName);
        Sprite emotionSprite = character != null ? character.GetEmotionSprite(emotion) : null;
        if (character == null || emotionSprite == null)
        {
            Debug.LogWarning($"Character \"{characterName}\" does not have a sprite for {emotion}.");
            return;
        }

        SpriteRenderer renderer = GetOrCreateCharacterRenderer(characterName);
        renderer.sprite = emotionSprite;
        renderer.enabled = true;
        currentCharacterEmotions[characterName] = emotion;
        LogVerbose($"[Emotion] {characterName}: {emotion}");
    }

    private bool AreVariableConditionsMet(IReadOnlyList<VariableCondition> variableConditions)
    {
        if (variableConditions == null || variableConditions.Count == 0)
        {
            return true;
        }

        if (playerController == null)
        {
            Debug.LogWarning("DialogueRunner has variable conditions, but PlayerController is not assigned.");
            return false;
        }

        for (int i = 0; i < variableConditions.Count; i++)
        {
            VariableCondition condition = variableConditions[i];
            playerController.TryGetVariableValue(condition.VariableName, out string currentValue);
            if (!CompareVariableValue(currentValue, condition.ComparisonOperator, condition.Value))
            {
                return false;
            }
        }

        return true;
    }

    private static bool CompareVariableValue(string currentValue, string comparisonOperator, string targetValue)
    {
        currentValue = currentValue ?? string.Empty;
        targetValue = targetValue ?? string.Empty;

        if (TryParseNumber(currentValue, out float currentNumber) &&
            TryParseNumber(targetValue, out float targetNumber))
        {
            switch (comparisonOperator)
            {
                case ">":
                    return currentNumber > targetNumber;
                case "<":
                    return currentNumber < targetNumber;
                case ">=":
                    return currentNumber >= targetNumber;
                case "<=":
                    return currentNumber <= targetNumber;
                case "==":
                    return Mathf.Approximately(currentNumber, targetNumber);
                case "!=":
                    return !Mathf.Approximately(currentNumber, targetNumber);
                default:
                    return false;
            }
        }

        switch (comparisonOperator)
        {
            case "==":
                return string.Equals(currentValue, targetValue, System.StringComparison.OrdinalIgnoreCase);
            case "!=":
                return !string.Equals(currentValue, targetValue, System.StringComparison.OrdinalIgnoreCase);
            default:
                return false;
        }
    }

    private static bool TryParseNumber(string value, out float number)
    {
        return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out number);
    }

    private void BuildGraphLookup(DialogueGraph graph)
    {
        hubsByName.Clear();
        nodesByName.Clear();

        for (int i = 0; i < graph.Nodes.Count; i++)
        {
            DialogueNode node = graph.Nodes[i];
            nodesByName[node.NodeName] = node;

            for (int j = 0; j < node.Elements.Count; j++)
            {
                if (node.Elements[j] is DialogueHub hub)
                {
                    hubsByName[hub.HubName] = hub;
                }
            }
        }
    }

    private void HandleCommand(DialogueCommand command)
    {
        switch (command.CommandType)
        {
            case DialogueCommandType.ShowCharacter:
                ShowCharacter(command.TargetName);
                break;
            case DialogueCommandType.HideCharacter:
                HideCharacter(command.TargetName);
                break;
            case DialogueCommandType.ShowBackground:
                ShowBackground(command.TargetName);
                break;
            case DialogueCommandType.HideBackground:
                HideBackground(command.TargetName);
                break;
            case DialogueCommandType.StartScene:
                StartScene(command.TargetName);
                break;
            case DialogueCommandType.StartNextPage:
                StartNextScreenTextPage();
                break;
        }
    }

    private string BuildScreenTextPage()
    {
        if (screenTextPageLines.Count == 0)
        {
            return string.Empty;
        }

        logBuilder.Length = 0;
        for (int i = 0; i < screenTextPageLines.Count; i++)
        {
            if (i > 0)
            {
                logBuilder.AppendLine();
                logBuilder.AppendLine();
            }

            logBuilder.Append(screenTextPageLines[i]);
        }

        return logBuilder.ToString();
    }

    private void StartNextScreenTextPage()
    {
        screenTextPageLines.Clear();
        if (dialogueUI != null)
        {
            dialogueUI.ClearScreenTextPage();
        }
    }

    private void StartScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("startScene command needs a scene name.");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    private WaitForSeconds WaitForLineDelay()
    {
        if (cachedLineDelay == null || !Mathf.Approximately(cachedLineDelaySeconds, lineDelaySeconds))
        {
            cachedLineDelaySeconds = lineDelaySeconds;
            cachedLineDelay = new WaitForSeconds(lineDelaySeconds);
        }

        return cachedLineDelay;
    }

    private bool WasNextPressed()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            return true;
        }

        return Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame &&
            !IsPointerOverUi();
    }

    private static bool IsPointerOverUi()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    private int GetPressedChoiceIndex(int choiceCount)
    {
        if (Keyboard.current == null)
        {
            return -1;
        }

        for (int i = 0; i < choiceCount && i < 9; i++)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame && i == 0 ||
                Keyboard.current.digit2Key.wasPressedThisFrame && i == 1 ||
                Keyboard.current.digit3Key.wasPressedThisFrame && i == 2 ||
                Keyboard.current.digit4Key.wasPressedThisFrame && i == 3 ||
                Keyboard.current.digit5Key.wasPressedThisFrame && i == 4 ||
                Keyboard.current.digit6Key.wasPressedThisFrame && i == 5 ||
                Keyboard.current.digit7Key.wasPressedThisFrame && i == 6 ||
                Keyboard.current.digit8Key.wasPressedThisFrame && i == 7 ||
                Keyboard.current.digit9Key.wasPressedThisFrame && i == 8)
            {
                return i;
            }
        }

        return -1;
    }

    private void ShowBackground(string backgroundName)
    {
        if (backgroundDatabase == null)
        {
            Debug.LogError("DialogueRunner needs a background database.");
            return;
        }

        BackgroundData background = backgroundDatabase.GetByName(backgroundName);
        if (background == null || background.Image == null)
        {
            Debug.LogError($"Background \"{backgroundName}\" was not found in the background database.");
            return;
        }

        SpriteRenderer renderer = GetOrCreateBackgroundRenderer();
        renderer.sprite = background.Image;
        renderer.enabled = true;
        currentBackgroundName = backgroundName;
        FitBackgroundToCamera(renderer);
        LogVerbose($"[Command] Show background: {backgroundName}");
    }

    private void HideBackground(string backgroundName)
    {
        if (backgroundRenderer != null)
        {
            backgroundRenderer.enabled = false;
        }

        if (currentBackgroundName == backgroundName)
        {
            currentBackgroundName = null;
        }

        LogVerbose($"[Command] Hide background: {backgroundName}");
    }

    private void ShowCharacter(string characterName)
    {
        if (characterDatabase == null)
        {
            Debug.LogError("DialogueRunner needs a character database.");
            return;
        }

        CharacterData character = characterDatabase.GetByName(characterName);
        Sprite characterSprite = character != null ? character.GetEmotionSprite(CharacterEmotion.Normal) : null;
        if (character == null || characterSprite == null)
        {
            Debug.LogError($"Character \"{characterName}\" was not found in the character database.");
            return;
        }

        SpriteRenderer renderer = GetOrCreateCharacterRenderer(characterName);
        renderer.sprite = characterSprite;
        renderer.enabled = true;
        visibleCharacterNames.Add(characterName);
        currentCharacterEmotions[characterName] = CharacterEmotion.Normal;
        PlaceCharacterAtBottomCenter(renderer);
        LogVerbose($"[Command] Show character: {characterName}");
    }

    private void HideCharacter(string characterName)
    {
        if (characterRenderers.TryGetValue(characterName, out SpriteRenderer renderer))
        {
            renderer.enabled = false;
        }

        visibleCharacterNames.Remove(characterName);
        currentCharacterEmotions.Remove(characterName);
        LogVerbose($"[Command] Hide character: {characterName}");
    }

    private List<CharacterEmotionSaveData> CaptureCharacterEmotionState()
    {
        List<CharacterEmotionSaveData> emotionStates = new List<CharacterEmotionSaveData>();

        foreach (string characterName in visibleCharacterNames)
        {
            currentCharacterEmotions.TryGetValue(characterName, out CharacterEmotion emotion);
            emotionStates.Add(new CharacterEmotionSaveData
            {
                characterName = characterName,
                emotionName = emotion.ToString()
            });
        }

        return emotionStates;
    }

    private SpriteRenderer GetOrCreateBackgroundRenderer()
    {
        if (backgroundRenderer != null)
        {
            return backgroundRenderer;
        }

        GameObject backgroundObject = new GameObject("Dialogue Background");
        backgroundObject.transform.SetParent(transform);
        backgroundRenderer = backgroundObject.AddComponent<SpriteRenderer>();
        backgroundRenderer.sortingOrder = -100;
        return backgroundRenderer;
    }

    private SpriteRenderer GetOrCreateCharacterRenderer(string characterName)
    {
        if (characterRenderers.TryGetValue(characterName, out SpriteRenderer renderer))
        {
            return renderer;
        }

        GameObject characterObject = new GameObject($"Dialogue Character - {characterName}");
        characterObject.transform.SetParent(transform);
        renderer = characterObject.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = 0;
        characterRenderers.Add(characterName, renderer);
        return renderer;
    }

    private void FitBackgroundToCamera(SpriteRenderer renderer)
    {
        Camera camera = GetTargetCamera();
        if (camera == null || renderer.sprite == null)
        {
            return;
        }

        float worldHeight = camera.orthographicSize * 2f;
        float worldWidth = worldHeight * camera.aspect;
        Vector2 spriteSize = renderer.sprite.bounds.size;
        float scale = Mathf.Max(worldWidth / spriteSize.x, worldHeight / spriteSize.y);

        renderer.transform.position = new Vector3(camera.transform.position.x, camera.transform.position.y, 0f);
        renderer.transform.localScale = new Vector3(scale, scale, 1f);
    }

    private void PlaceCharacterAtBottomCenter(SpriteRenderer renderer)
    {
        Camera camera = GetTargetCamera();
        if (camera == null || renderer.sprite == null)
        {
            return;
        }

        float worldHeight = camera.orthographicSize * 2f;
        Vector2 spriteSize = renderer.sprite.bounds.size;
        float scale = worldHeight * characterScreenHeight / spriteSize.y;
        float bottomY = camera.transform.position.y - camera.orthographicSize;
        float centerY = bottomY + spriteSize.y * scale * 0.5f;

        renderer.transform.position = new Vector3(camera.transform.position.x, centerY, -1f);
        renderer.transform.localScale = new Vector3(scale, scale, 1f);
    }

    private Camera GetTargetCamera()
    {
        if (targetCamera != null)
        {
            return targetCamera;
        }

        targetCamera = Camera.main;
        if (targetCamera == null)
        {
            targetCamera = FindAnyObjectByType<Camera>();
        }

        return targetCamera;
    }

    private void LogVerbose(string message)
    {
        if (verboseLogging)
        {
            Debug.Log(message);
        }
    }
}
