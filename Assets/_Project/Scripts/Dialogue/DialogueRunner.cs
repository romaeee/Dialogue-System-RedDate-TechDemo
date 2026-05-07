using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class DialogueRunner : MonoBehaviour
{
    [SerializeField] private TextAsset dialogueText;
    [SerializeField] private CharacterDatabase characterDatabase;
    [SerializeField] private BackgroundDatabase backgroundDatabase;
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private float lineDelaySeconds = 1f;
    [SerializeField] private bool autoSelectFirstChoice = true;
    [SerializeField] private float characterScreenHeight = 0.75f;

    private Coroutine playRoutine;
    private Camera targetCamera;
    private SpriteRenderer backgroundRenderer;
    private readonly Dictionary<string, SpriteRenderer> characterRenderers = new Dictionary<string, SpriteRenderer>();

    private void Start()
    {
        if (playOnStart)
        {
            Play();
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

        DialogueGraph graph = DialogueParser.Parse(dialogueText);
        playRoutine = StartCoroutine(PlayGraph(graph));
    }

    private IEnumerator PlayGraph(DialogueGraph graph)
    {
        if (graph.StartNode == null)
        {
            yield break;
        }

        Debug.Log("[Dialogue] Start");
        yield return PlayNode(graph.StartNode);
        Debug.Log("[Dialogue] End");
        playRoutine = null;
    }

    private IEnumerator PlayNode(DialogueNode node)
    {
        for (int i = 0; i < node.Elements.Count; i++)
        {
            DialogueElement element = node.Elements[i];

            if (element is DialogueLine line)
            {
                Debug.Log($"{line.SpeakerName}: {line.Text}");
                yield return WaitForLineDelay();
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

        for (int i = 0; i < hub.Choices.Count; i++)
        {
            DialogueChoice choice = hub.Choices[i];
            Debug.Log($"[Choice {i + 1}] {choice.BoxText}");
        }

        if (!autoSelectFirstChoice || hub.Choices.Count == 0)
        {
            yield break;
        }

        yield return WaitForLineDelay();

        DialogueChoice selectedChoice = hub.Choices[0];
        Debug.Log($"{selectedChoice.SpeakerName}: {selectedChoice.SelectedText}");
        yield return WaitForLineDelay();
        yield return PlayNode(selectedChoice.ConsequenceNode);
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
        return new WaitForSeconds(lineDelaySeconds);
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
        FitBackgroundToCamera(renderer);
        Debug.Log($"[Command] Show background: {backgroundName}");
    }

    private void HideBackground(string backgroundName)
    {
        if (backgroundRenderer != null)
        {
            backgroundRenderer.enabled = false;
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
        PlaceCharacterAtBottomCenter(renderer);
        Debug.Log($"[Command] Show character: {characterName}");
    }

    private void HideCharacter(string characterName)
    {
        if (characterRenderers.TryGetValue(characterName, out SpriteRenderer renderer))
        {
            renderer.enabled = false;
        }

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
