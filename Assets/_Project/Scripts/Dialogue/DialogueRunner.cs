using System.Collections;
using UnityEngine;

public sealed class DialogueRunner : MonoBehaviour
{
    [SerializeField] private TextAsset dialogueText;
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private float lineDelaySeconds = 1f;
    [SerializeField] private bool autoSelectFirstChoice = true;

    private Coroutine playRoutine;

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
                LogCommand(command);
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

    private void LogCommand(DialogueCommand command)
    {
        switch (command.CommandType)
        {
            case DialogueCommandType.ShowCharacter:
                Debug.Log($"[Command] Show character: {command.TargetName}");
                break;
            case DialogueCommandType.HideCharacter:
                Debug.Log($"[Command] Hide character: {command.TargetName}");
                break;
            case DialogueCommandType.ShowBackground:
                Debug.Log($"[Command] Show background: {command.TargetName}");
                break;
            case DialogueCommandType.HideBackground:
                Debug.Log($"[Command] Hide background: {command.TargetName}");
                break;
        }
    }

    private WaitForSeconds WaitForLineDelay()
    {
        return new WaitForSeconds(lineDelaySeconds);
    }
}
