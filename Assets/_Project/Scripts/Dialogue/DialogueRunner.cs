using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
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
    [SerializeField] private bool playOnStart = true;
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
    private int currentElementIndex;
    private string currentBackgroundName;
    private readonly Dictionary<string, DialogueHub> hubsByName = new Dictionary<string, DialogueHub>();
    private readonly Dictionary<string, DialogueNode> nodesByName = new Dictionary<string, DialogueNode>();
    private readonly HashSet<int> usedOnceChoiceLineNumbers = new HashSet<int>();
    private readonly HashSet<string> visibleCharacterNames = new HashSet<string>();
    private readonly Dictionary<string, SpriteRenderer> characterRenderers = new Dictionary<string, SpriteRenderer>();

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
        StartGraph(graph);
    }

    public DialogueSaveData CaptureState()
    {
        DialogueSaveData state = new DialogueSaveData
        {
            nodeName = currentNodeName,
            elementIndex = currentElementIndex,
            currentBackgroundName = currentBackgroundName,
            visibleCharacterNames = new List<string>(visibleCharacterNames),
            usedOnceChoiceLineNumbers = new List<int>(usedOnceChoiceLineNumbers)
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

        if (!nodesByName.TryGetValue(state.nodeName, out DialogueNode node))
        {
            Debug.LogWarning($"Saved dialogue node \"{state.nodeName}\" was not found. Loading from start.");
            node = graph.StartNode;
        }

        int startIndex = Mathf.Clamp(state.elementIndex, 0, Mathf.Max(0, node.Elements.Count - 1));
        currentNodeName = node.NodeName;
        currentElementIndex = startIndex;

        EnsureDialogueUI();

        playRoutine = StartCoroutine(PlayNode(node, startIndex));
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

        dialogueUI = new DialogueUI(transform);
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
        visibleCharacterNames.Clear();
        usedOnceChoiceLineNumbers.Clear();

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

    private IEnumerator PlayGraph(DialogueGraph graph)
    {
        if (graph.StartNode == null)
        {
            yield break;
        }

        Debug.Log("[Dialogue] Start");
        yield return PlayNode(graph.StartNode, 0);
        Debug.Log("[Dialogue] End");
        playRoutine = null;
    }

    private IEnumerator PlayNode(DialogueNode node, int startIndex)
    {
        currentNodeName = node.NodeName;

        for (int i = startIndex; i < node.Elements.Count; i++)
        {
            currentNodeName = node.NodeName;
            currentElementIndex = i;
            DialogueElement element = node.Elements[i];

            if (element is DialogueLine line)
            {
                Debug.Log($"{line.SpeakerName}: {line.Text}");
                ApplyRelationshipChanges(line.RelationshipChanges);
                bool hideAfterAdvance = !(i + 1 < node.Elements.Count && node.Elements[i + 1] is DialogueHub);
                yield return dialogueUI.ShowLine(line, typewriterCharactersPerSecond, WasNextPressed, hideAfterAdvance);
                continue;
            }

            if (element is DialogueCommand command)
            {
                HandleCommand(command);
                yield return WaitForLineDelay();
                continue;
            }

            if (element is DialogueHub hub)
            {
                yield return PlayHub(hub);
            }
        }
    }

    private IEnumerator PlayHub(DialogueHub hub)
    {
        Debug.Log($"[Hub] {hub.HubName}");
        List<DialogueChoice> availableChoices = GetAvailableChoices(hub);

        for (int i = 0; i < availableChoices.Count; i++)
        {
            DialogueChoice choice = availableChoices[i];
            Debug.Log($"[Choice {i + 1}] {choice.BoxText}");
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
        dialogueUI.HideChoices();

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

        Debug.Log($"{selectedChoice.SpeakerName}: {selectedChoice.SelectedText}");
        yield return dialogueUI.ShowLine(
            new DialogueLine(selectedChoice.LineNumber, selectedChoice.SpeakerName, selectedChoice.SelectedText),
            typewriterCharactersPerSecond,
            WasNextPressed);
        yield return PlayNode(selectedChoice.ConsequenceNode, 0);
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

            availableChoices.Add(choice);
        }

        return availableChoices;
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
        }
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
        return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
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
        Debug.Log($"[Command] Show background: {backgroundName}");
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

        Debug.Log($"[Command] Hide background: {backgroundName}");
    }

    private void ShowCharacter(string characterName)
    {
        if (characterDatabase == null)
        {
            Debug.LogError("DialogueRunner needs a character database.");
            return;
        }

        CharacterData character = characterDatabase.GetByName(characterName);
        if (character == null || character.Image == null)
        {
            Debug.LogError($"Character \"{characterName}\" was not found in the character database.");
            return;
        }

        SpriteRenderer renderer = GetOrCreateCharacterRenderer(characterName);
        renderer.sprite = character.Image;
        renderer.enabled = true;
        visibleCharacterNames.Add(characterName);
        PlaceCharacterAtBottomCenter(renderer);
        Debug.Log($"[Command] Show character: {characterName}");
    }

    private void HideCharacter(string characterName)
    {
        if (characterRenderers.TryGetValue(characterName, out SpriteRenderer renderer))
        {
            renderer.enabled = false;
        }

        visibleCharacterNames.Remove(characterName);
        Debug.Log($"[Command] Hide character: {characterName}");
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
}
