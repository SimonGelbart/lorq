namespace Lorq.Core;

internal static class LorqBenchmarkExpectedCells
{
    public static IReadOnlyList<string> ReadFrom(string benchmarkPath)
    {
        var path = Path.GetFullPath(benchmarkPath);
        if (!File.Exists(path))
        {
            throw new LorqPackageFormatException($"Benchmark file does not exist: {path}.");
        }

        var lines = File.ReadAllLines(path);
        var cases = ReadListIds(lines, "cases");
        var modes = ReadListIds(lines, "modes");
        var attemptCount = ReadAttemptsPerCaseMode(lines);
        return ExpectedCells(cases, modes, attemptCount);
    }

    private static IReadOnlyList<string> ExpectedCells(IReadOnlyList<string> cases, IReadOnlyList<string> modes, int attemptCount)
    {
        var cells = new List<string>();
        foreach (var caseId in cases)
        {
            AddCellsForCase(cells, caseId, modes, attemptCount);
        }

        return cells.Order(StringComparer.Ordinal).ToArray();
    }

    private static void AddCellsForCase(List<string> cells, string caseId, IReadOnlyList<string> modes, int attemptCount)
    {
        foreach (var modeId in modes)
        {
            AddAttempts(cells, caseId, modeId, attemptCount);
        }
    }

    private static void AddAttempts(List<string> cells, string caseId, string modeId, int attemptCount)
    {
        for (var attempt = 1; attempt <= attemptCount; attempt++)
        {
            cells.Add($"{Slug(caseId)}__{Slug(modeId)}__attempt-{attempt:000}");
        }
    }

    private static IReadOnlyList<string> ReadListIds(IReadOnlyList<string> lines, string sectionName)
    {
        var ids = new List<string>();
        var insideSection = false;
        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (IsTopLevelSection(rawLine, sectionName))
            {
                insideSection = true;
                continue;
            }

            if (insideSection && IsNextTopLevelSection(rawLine))
            {
                break;
            }

            AddSectionId(ids, insideSection, line);
        }

        return ids;
    }

    private static void AddSectionId(List<string> ids, bool insideSection, string line)
    {
        if (!insideSection || !line.StartsWith("- id:", StringComparison.Ordinal))
        {
            return;
        }

        var value = line[5..].Trim().Trim('"', '\'');
        if (value.Length > 0)
        {
            ids.Add(value);
        }
    }

    private static bool IsTopLevelSection(string rawLine, string sectionName)
    {
        return rawLine == sectionName + ":";
    }

    private static bool IsNextTopLevelSection(string rawLine)
    {
        return rawLine.Length > 0 && !char.IsWhiteSpace(rawLine[0]) && rawLine.Contains(':', StringComparison.Ordinal);
    }

    private static int ReadAttemptsPerCaseMode(IReadOnlyList<string> lines)
    {
        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (!line.StartsWith("attempts_per_case_mode:", StringComparison.Ordinal))
            {
                continue;
            }

            var value = line.Split(':', 2)[1].Trim();
            return int.TryParse(value, out var parsed) ? parsed : 1;
        }

        return 1;
    }

    private static string Slug(string value)
    {
        var characters = new List<char>();
        foreach (var character in value.Trim().ToLowerInvariant())
        {
            AddSlugCharacter(characters, character);
        }

        return CompactSlug(new string(characters.ToArray()));
    }

    private static void AddSlugCharacter(List<char> characters, char character)
    {
        if (char.IsLetterOrDigit(character) || character is '-' or '_' or '.')
        {
            characters.Add(character);
            return;
        }

        if (char.IsWhiteSpace(character) || character is '/' or ':' or ',')
        {
            characters.Add('-');
        }
    }

    private static string CompactSlug(string value)
    {
        var slug = value.Trim('-', '.', '_');
        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }

        return slug.Length == 0 ? "run" : slug;
    }
}
