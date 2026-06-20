namespace Lorq.Cli.Runtime;

internal sealed class RunPromptBuilder
{
    public string Build(string casePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(casePath);
        return "You are evaluating repository evidence. Use only the provided repository context."
            + Environment.NewLine
            + Environment.NewLine
            + "Task:"
            + Environment.NewLine
            + CaseTask(casePath);
    }

    private static string CaseTask(string casePath)
    {
        var task = new List<string>();
        var inside = false;
        foreach (var rawLine in File.ReadAllLines(casePath))
        {
            if (rawLine == "task: |")
            {
                inside = true;
                continue;
            }

            if (inside && rawLine.Length > 0 && !char.IsWhiteSpace(rawLine[0]))
            {
                break;
            }

            if (inside)
            {
                task.Add(rawLine.StartsWith("  ", StringComparison.Ordinal) ? rawLine[2..] : rawLine);
            }
        }

        return string.Join(Environment.NewLine, task).TrimEnd() + Environment.NewLine;
    }
}
