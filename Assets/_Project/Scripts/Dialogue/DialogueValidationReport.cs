using System.Collections.Generic;
using System.Text;
using UnityEngine;

public sealed class DialogueValidationReport
{
    private readonly List<DialogueValidationIssue> issues = new List<DialogueValidationIssue>();

    public IReadOnlyList<DialogueValidationIssue> Issues => issues;
    public bool IsValid => ErrorCount == 0;
    public int ErrorCount { get; private set; }
    public int WarningCount { get; private set; }

    public void Add(DialogueValidationSeverity severity, int lineNumber, string message)
    {
        DialogueValidationIssue issue = new DialogueValidationIssue(severity, lineNumber, message);
        issues.Add(issue);

        if (severity == DialogueValidationSeverity.Error)
        {
            ErrorCount++;
        }
        else if (severity == DialogueValidationSeverity.Warning)
        {
            WarningCount++;
        }
    }

    public void LogToConsole(string dialogueName)
    {
        string summary = BuildSummary(dialogueName);

        if (ErrorCount > 0)
        {
            Debug.LogError(summary);
            return;
        }

        if (WarningCount > 0)
        {
            Debug.LogWarning(summary);
            return;
        }

        Debug.Log(summary);
    }

    private string BuildSummary(string dialogueName)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append("Dialogue validation ");
        builder.Append(IsValid ? "passed" : "failed");

        if (!string.IsNullOrWhiteSpace(dialogueName))
        {
            builder.Append(": ");
            builder.Append(dialogueName);
        }

        builder.Append(" (");
        builder.Append(ErrorCount);
        builder.Append(" errors, ");
        builder.Append(WarningCount);
        builder.Append(" warnings)");

        for (int i = 0; i < issues.Count; i++)
        {
            builder.AppendLine();
            builder.Append(issues[i]);
        }

        return builder.ToString();
    }
}
