namespace Lorq.Adapters.Process;

internal static class DeterministicFakeAgentFixtureParser
{
    public static IReadOnlyDictionary<string, DeterministicFakeAgentCell> Parse(string fixturePath)
    {
        var lines = File.ReadAllLines(fixturePath);
        var cells = new Dictionary<string, DeterministicFakeAgentCell>(StringComparer.Ordinal);
        var index = 0;

        while (index < lines.Length)
        {
            if (!lines[index].StartsWith("- case:", StringComparison.Ordinal))
            {
                index++;
                continue;
            }

            var start = index;
            index++;
            while (index < lines.Length && !lines[index].StartsWith("- case:", StringComparison.Ordinal))
            {
                index++;
            }

            var cell = ParseCell(lines[start..index]);
            cells[DeterministicFakeAgentCell.Key(cell.CaseId, cell.ModeId, cell.Attempt)] = cell;
        }

        return cells;
    }

    private static DeterministicFakeAgentCell ParseCell(IReadOnlyList<string> lines)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        var usage = new Dictionary<string, long>(StringComparer.Ordinal);
        var events = new List<IReadOnlyDictionary<string, string>>();
        var artifacts = new List<DeterministicFakeArtifact>();
        var integrityWarnings = new List<string>();
        string? activeScalar = null;

        for (var index = 0; index < lines.Count; index++)
        {
            var line = lines[index];
            var trimmed = line.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            if (TryReadTopValue(trimmed, values, ref activeScalar))
            {
                continue;
            }

            if (activeScalar is not null && IsContinuationLine(line))
            {
                values[activeScalar] = values[activeScalar] + " " + trimmed;
                continue;
            }

            if (trimmed == "usage:")
            {
                index = ReadUsage(lines, index + 1, usage) - 1;
                activeScalar = null;
                continue;
            }

            if (trimmed == "events:")
            {
                index = ReadEvents(lines, index + 1, events) - 1;
                activeScalar = null;
                continue;
            }

            if (trimmed == "artifacts:")
            {
                index = ReadArtifacts(lines, index + 1, artifacts) - 1;
                activeScalar = null;
                continue;
            }

            if (trimmed == "integrity_warnings:")
            {
                index = ReadWarnings(lines, index + 1, integrityWarnings) - 1;
                activeScalar = null;
            }
        }

        return new DeterministicFakeAgentCell(
            Required(values, "case"),
            Required(values, "mode"),
            IntValue(values, "attempt", 1),
            Required(values, "status"),
            IntValue(values, "elapsed_ms", 0),
            values.GetValueOrDefault("final_answer", string.Empty),
            IntValue(values, "exit_code", 0),
            BoolValue(values, "timed_out", false),
            values.GetValueOrDefault("error_category"),
            usage,
            events,
            artifacts,
            integrityWarnings);
    }

    private static bool TryReadTopValue(string trimmed, Dictionary<string, string> values, ref string? activeScalar)
    {
        var content = trimmed.StartsWith("- ", StringComparison.Ordinal) ? trimmed[2..] : trimmed;
        var separator = content.IndexOf(':', StringComparison.Ordinal);
        if (separator <= 0)
        {
            return false;
        }

        var key = content[..separator].Trim();
        if (!IsCellScalar(key))
        {
            return false;
        }

        var value = content[(separator + 1)..].Trim().Trim('"', '\'');
        values[key] = value;
        activeScalar = key == "final_answer" ? key : null;
        return true;
    }

    private static bool IsCellScalar(string key)
    {
        return key is "case" or "mode" or "attempt" or "status" or "elapsed_ms" or "final_answer" or "exit_code" or "timed_out" or "error_category";
    }

    private static bool IsContinuationLine(string line)
    {
        var trimmed = line.Trim();
        return line.StartsWith("    ", StringComparison.Ordinal)
            && !trimmed.StartsWith("- ", StringComparison.Ordinal)
            && !trimmed.Contains(':', StringComparison.Ordinal);
    }

    private static int ReadUsage(IReadOnlyList<string> lines, int index, Dictionary<string, long> usage)
    {
        while (index < lines.Count && lines[index].StartsWith("    ", StringComparison.Ordinal))
        {
            var pair = SplitPair(lines[index].Trim());
            if (pair.Key.Length > 0 && long.TryParse(pair.Value, out var parsed))
            {
                usage[pair.Key] = parsed;
            }

            index++;
        }

        return index;
    }

    private static int ReadEvents(IReadOnlyList<string> lines, int index, List<IReadOnlyDictionary<string, string>> events)
    {
        Dictionary<string, string>? current = null;
        while (index < lines.Count && !IsTopCellSection(lines[index]))
        {
            var trimmed = lines[index].Trim();
            if (trimmed.StartsWith("- ", StringComparison.Ordinal))
            {
                AddCurrent(events, ref current);
                current = new Dictionary<string, string>(StringComparer.Ordinal);
                AddPair(current, trimmed[2..]);
            }
            else if (current is not null && lines[index].StartsWith("    ", StringComparison.Ordinal))
            {
                AddPair(current, trimmed);
            }

            index++;
        }

        AddCurrent(events, ref current);
        return index;
    }

    private static int ReadArtifacts(IReadOnlyList<string> lines, int index, List<DeterministicFakeArtifact> artifacts)
    {
        Dictionary<string, string>? current = null;
        while (index < lines.Count && !IsTopCellSection(lines[index]))
        {
            var trimmed = lines[index].Trim();
            if (trimmed.StartsWith("- ", StringComparison.Ordinal))
            {
                AddArtifact(artifacts, ref current);
                current = new Dictionary<string, string>(StringComparer.Ordinal);
                AddPair(current, trimmed[2..]);
            }
            else if (current is not null && lines[index].StartsWith("    ", StringComparison.Ordinal))
            {
                AddPair(current, trimmed);
            }

            index++;
        }

        AddArtifact(artifacts, ref current);
        return index;
    }

    private static int ReadWarnings(IReadOnlyList<string> lines, int index, List<string> warnings)
    {
        while (index < lines.Count && !IsTopCellSection(lines[index]))
        {
            var trimmed = lines[index].Trim();
            if (trimmed == "[]")
            {
                index++;
                continue;
            }

            if (trimmed.StartsWith("- ", StringComparison.Ordinal))
            {
                warnings.Add(trimmed[2..].Trim());
            }

            index++;
        }

        return index;
    }

    private static bool IsTopCellSection(string line)
    {
        var trimmed = line.Trim();
        return line.StartsWith("- case:", StringComparison.Ordinal)
            || (line.StartsWith("  ", StringComparison.Ordinal) && !line.StartsWith("    ", StringComparison.Ordinal) && trimmed.EndsWith(':'));
    }

    private static void AddCurrent(List<IReadOnlyDictionary<string, string>> events, ref Dictionary<string, string>? current)
    {
        if (current is not null)
        {
            events.Add(current);
            current = null;
        }
    }

    private static void AddArtifact(List<DeterministicFakeArtifact> artifacts, ref Dictionary<string, string>? current)
    {
        if (current is not null && current.TryGetValue("path", out var path))
        {
            artifacts.Add(new DeterministicFakeArtifact(path, current.GetValueOrDefault("kind", "artifact")));
        }

        current = null;
    }

    private static void AddPair(Dictionary<string, string> values, string text)
    {
        var pair = SplitPair(text);
        if (pair.Key.Length > 0)
        {
            values[pair.Key] = pair.Value;
        }
    }

    private static (string Key, string Value) SplitPair(string text)
    {
        var separator = text.IndexOf(':', StringComparison.Ordinal);
        if (separator <= 0)
        {
            return (string.Empty, string.Empty);
        }

        return (text[..separator].Trim(), text[(separator + 1)..].Trim().Trim('"', '\''));
    }

    private static string Required(IReadOnlyDictionary<string, string> values, string key)
    {
        if (!values.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Missing fake agent fixture field '{key}'.");
        }

        return value;
    }

    private static int IntValue(IReadOnlyDictionary<string, string> values, string key, int fallback)
    {
        return values.TryGetValue(key, out var value) && int.TryParse(value, out var parsed) ? parsed : fallback;
    }

    private static bool BoolValue(IReadOnlyDictionary<string, string> values, string key, bool fallback)
    {
        return values.TryGetValue(key, out var value) && bool.TryParse(value, out var parsed) ? parsed : fallback;
    }
}
