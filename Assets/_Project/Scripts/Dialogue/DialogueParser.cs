using System;
using System.Collections.Generic;
using UnityEngine;

public static class DialogueParser
{
    public static DialogueGraph Parse(TextAsset textAsset)
    {
        if (textAsset == null)
        {
            throw new ArgumentNullException(nameof(textAsset));
        }

        return Parse(textAsset.text);
    }

    public static DialogueGraph Parse(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        List<ParsedLine> lines = BuildParsedLines(text);
        int index = 0;
        List<DialogueNode> nodes = new List<DialogueNode>();
        DialogueNode startNode = new DialogueNode("Start", ParseBlock(lines, ref index, 0, nodes));
        nodes.Insert(0, startNode);

        return new DialogueGraph(nodes);
    }

    private static List<DialogueElement> ParseBlock(List<ParsedLine> lines, ref int index, int indent, List<DialogueNode> nodes)
    {
        List<DialogueElement> elements = new List<DialogueElement>();

        while (index < lines.Count)
        {
            ParsedLine line = lines[index];

            if (line.Indent < indent)
            {
                break;
            }

            if (line.Indent > indent)
            {
                throw new DialogueParseException(line.LineNumber, $"Unexpected tab indent. Expected {indent}, got {line.Indent}.");
            }

            if (TryParseHubHeader(line.Text, out string hubName))
            {
                elements.Add(ParseHub(lines, ref index, line.Indent, hubName, nodes));
                continue;
            }

            if (TryParseCommand(line.Text, line.LineNumber, out DialogueCommand command))
            {
                elements.Add(command);
                index++;
                continue;
            }

            if (TryParseDialogueLine(line.Text, line.LineNumber, out DialogueLine dialogueLine))
            {
                elements.Add(dialogueLine);
                index++;
                continue;
            }

            throw new DialogueParseException(line.LineNumber, $"Could not parse line: \"{line.Text}\".");
        }

        return elements;
    }

    private static DialogueHub ParseHub(List<ParsedLine> lines, ref int index, int hubIndent, string hubName, List<DialogueNode> nodes)
    {
        int hubLineNumber = lines[index].LineNumber;
        index++;

        List<DialogueChoice> choices = new List<DialogueChoice>();

        while (index < lines.Count)
        {
            ParsedLine line = lines[index];

            if (line.Indent < hubIndent)
            {
                break;
            }

            if (line.Indent > hubIndent)
            {
                throw new DialogueParseException(line.LineNumber, $"Expected a choice option at tab indent {hubIndent}.");
            }

            if (!TryParseOption(line.Text, line.LineNumber, out DialogueOptionHeader optionHeader))
            {
                break;
            }

            index++;
            List<DialogueElement> consequences = ParseBlock(lines, ref index, hubIndent + 1, nodes);
            DialogueNode consequenceNode = new DialogueNode(
                $"{hubName}/{optionHeader.BoxText}",
                consequences);

            nodes.Add(consequenceNode);
            choices.Add(new DialogueChoice(
                optionHeader.LineNumber,
                optionHeader.SpeakerName,
                optionHeader.BoxText,
                optionHeader.SelectedText,
                consequenceNode,
                optionHeader.TargetHubName));
        }

        if (choices.Count == 0)
        {
            throw new DialogueParseException(hubLineNumber, $"Hub \"{hubName}\" does not have any options.");
        }

        return new DialogueHub(hubLineNumber, hubName, choices);
    }

    private static List<ParsedLine> BuildParsedLines(string text)
    {
        string[] rawLines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        List<ParsedLine> parsedLines = new List<ParsedLine>();

        for (int i = 0; i < rawLines.Length; i++)
        {
            string rawLine = rawLines[i];

            if (string.IsNullOrWhiteSpace(rawLine))
            {
                continue;
            }

            int indent = CountLeadingTabs(rawLine);
            string lineText = rawLine.Substring(indent).Trim();
            parsedLines.Add(new ParsedLine(i + 1, indent, lineText));
        }

        return parsedLines;
    }

    private static int CountLeadingTabs(string line)
    {
        int count = 0;
        while (count < line.Length && line[count] == '\t')
        {
            count++;
        }

        return count;
    }

    private static bool TryParseHubHeader(string text, out string hubName)
    {
        hubName = null;

        if (!text.StartsWith("Hub ", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        hubName = text.Substring(4).Trim();
        return !string.IsNullOrWhiteSpace(hubName);
    }

    private static bool TryParseCommand(string text, int lineNumber, out DialogueCommand command)
    {
        command = null;
        int separatorIndex = text.IndexOf(':');

        if (separatorIndex < 0)
        {
            return false;
        }

        string commandName = text.Substring(0, separatorIndex).Trim();
        string targetName = text.Substring(separatorIndex + 1).Trim();

        if (!TryGetCommandType(commandName, out DialogueCommandType commandType))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(targetName))
        {
            throw new DialogueParseException(lineNumber, $"Command \"{commandName}\" needs a target name.");
        }

        command = new DialogueCommand(lineNumber, commandType, targetName);
        return true;
    }

    private static bool TryGetCommandType(string commandName, out DialogueCommandType commandType)
    {
        switch (commandName)
        {
            case "showCharacter":
                commandType = DialogueCommandType.ShowCharacter;
                return true;
            case "hideCharacter":
                commandType = DialogueCommandType.HideCharacter;
                return true;
            case "showBackground":
            case "showScene":
                commandType = DialogueCommandType.ShowBackground;
                return true;
            case "hideBackground":
                commandType = DialogueCommandType.HideBackground;
                return true;
            default:
                commandType = default;
                return false;
        }
    }

    private static bool TryParseDialogueLine(string text, int lineNumber, out DialogueLine dialogueLine)
    {
        dialogueLine = null;
        int separatorIndex = text.IndexOf(':');

        if (separatorIndex < 0)
        {
            return false;
        }

        string speakerName = text.Substring(0, separatorIndex).Trim();
        string dialogueText = text.Substring(separatorIndex + 1).Trim();

        if (string.IsNullOrWhiteSpace(speakerName) || string.IsNullOrWhiteSpace(dialogueText))
        {
            throw new DialogueParseException(lineNumber, "Dialogue lines need both a speaker and text.");
        }

        dialogueLine = new DialogueLine(lineNumber, speakerName, dialogueText);
        return true;
    }

    private static bool TryParseOption(string text, int lineNumber, out DialogueOptionHeader optionHeader)
    {
        optionHeader = null;
        int separatorIndex = text.IndexOf(':');

        if (separatorIndex < 0)
        {
            string linkChoiceText = text.Trim();
            if (!TryExtractHubTarget(ref linkChoiceText, out string linkTargetHubName))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(linkChoiceText))
            {
                throw new DialogueParseException(lineNumber, "Linked choice needs button text before the [back/go Hub] target.");
            }

            optionHeader = new DialogueOptionHeader(lineNumber, string.Empty, linkChoiceText, string.Empty, linkTargetHubName);
            return true;
        }

        string speakerName = text.Substring(0, separatorIndex).Trim();
        string optionText = text.Substring(separatorIndex + 1).Trim();
        TryExtractHubTarget(ref optionText, out string targetHubName);

        int closeIndex = optionText.LastIndexOf(')');
        int openIndex = optionText.LastIndexOf('(');

        if (openIndex < 0 || closeIndex != optionText.Length - 1 || openIndex > closeIndex)
        {
            return false;
        }

        string boxText = optionText.Substring(0, openIndex).Trim();
        string selectedText = optionText.Substring(openIndex + 1, closeIndex - openIndex - 1).Trim();

        if (string.IsNullOrWhiteSpace(speakerName) ||
            string.IsNullOrWhiteSpace(boxText) ||
            string.IsNullOrWhiteSpace(selectedText))
        {
            throw new DialogueParseException(lineNumber, "Choice options need speaker, box text, and selected text.");
        }

        optionHeader = new DialogueOptionHeader(lineNumber, speakerName, boxText, selectedText, targetHubName);
        return true;
    }

    private static bool TryExtractHubTarget(ref string text, out string targetHubName)
    {
        targetHubName = null;
        string trimmedText = text.Trim();

        if (!trimmedText.EndsWith("]", StringComparison.Ordinal))
        {
            text = trimmedText;
            return false;
        }

        int openIndex = trimmedText.LastIndexOf('[');
        if (openIndex < 0)
        {
            text = trimmedText;
            return false;
        }

        string commandText = trimmedText.Substring(openIndex + 1, trimmedText.Length - openIndex - 2).Trim();

        if (TryReadHubTarget(commandText, "back ", out targetHubName) ||
            TryReadHubTarget(commandText, "go ", out targetHubName))
        {
            text = trimmedText.Substring(0, openIndex).Trim();
            return true;
        }

        text = trimmedText;
        return false;
    }

    private static bool TryReadHubTarget(string commandText, string prefix, out string targetHubName)
    {
        targetHubName = null;

        if (!commandText.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        targetHubName = commandText.Substring(prefix.Length).Trim();
        if (targetHubName.StartsWith("Hub ", StringComparison.OrdinalIgnoreCase))
        {
            targetHubName = targetHubName.Substring(4).Trim();
        }

        return !string.IsNullOrWhiteSpace(targetHubName);
    }

    private sealed class ParsedLine
    {
        public ParsedLine(int lineNumber, int indent, string text)
        {
            LineNumber = lineNumber;
            Indent = indent;
            Text = text;
        }

        public int LineNumber { get; }
        public int Indent { get; }
        public string Text { get; }
    }

    private sealed class DialogueOptionHeader
    {
        public DialogueOptionHeader(int lineNumber, string speakerName, string boxText, string selectedText, string targetHubName = null)
        {
            LineNumber = lineNumber;
            SpeakerName = speakerName;
            BoxText = boxText;
            SelectedText = selectedText;
            TargetHubName = targetHubName;
        }

        public int LineNumber { get; }
        public string SpeakerName { get; }
        public string BoxText { get; }
        public string SelectedText { get; }
        public string TargetHubName { get; }
    }
}
