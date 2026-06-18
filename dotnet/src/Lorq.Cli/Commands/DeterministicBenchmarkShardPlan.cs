namespace Lorq.Cli.Commands;

internal sealed record DeterministicBenchmarkShardPlan(string ShardId, IReadOnlyList<DeterministicBenchmarkCell> Cells)
{
    public static DeterministicBenchmarkShardPlan ReadFrom(string benchmarkPath, string shardId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(benchmarkPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(shardId);
        var path = Path.GetFullPath(benchmarkPath);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Benchmark file does not exist.", path);
        }

        return DeterministicBenchmarkShardPlanParser.Read(File.ReadAllLines(path), shardId);
    }
}

internal sealed record DeterministicBenchmarkCell(string CaseId, string ModeId, int Attempt, string Category);

internal static class DeterministicBenchmarkShardPlanParser
{
    public static DeterministicBenchmarkShardPlan Read(IReadOnlyList<string> lines, string shardId)
    {
        var attempts = ReadAttempts(lines);
        var cells = new List<DeterministicBenchmarkCell>();
        var insidePlannedShards = false;
        var insideTargetShard = false;
        IReadOnlyList<string> pendingCases = Array.Empty<string>();

        foreach (var rawLine in lines)
        {
            if (rawLine == "planned_shards:")
            {
                insidePlannedShards = true;
                continue;
            }

            if (insidePlannedShards && IsNextTopLevelSection(rawLine))
            {
                break;
            }

            if (!insidePlannedShards)
            {
                continue;
            }

            var trimmed = rawLine.Trim();
            if (trimmed.StartsWith("- id:", StringComparison.Ordinal))
            {
                insideTargetShard = trimmed[5..].Trim() == shardId;
                pendingCases = Array.Empty<string>();
                continue;
            }

            if (!insideTargetShard)
            {
                continue;
            }

            if (trimmed.StartsWith("cases:", StringComparison.Ordinal))
            {
                pendingCases = ReadInlineList(trimmed);
                continue;
            }

            if (trimmed.StartsWith("modes:", StringComparison.Ordinal))
            {
                AddCells(cells, pendingCases, ReadInlineList(trimmed), attempts);
                pendingCases = Array.Empty<string>();
            }
        }

        if (cells.Count == 0)
        {
            throw new InvalidOperationException($"Benchmark does not define planned shard '{shardId}'.");
        }

        return new DeterministicBenchmarkShardPlan(shardId, cells);
    }

    private static void AddCells(List<DeterministicBenchmarkCell> cells, IReadOnlyList<string> caseIds, IReadOnlyList<string> modeIds, int attempts)
    {
        foreach (var caseId in caseIds)
        foreach (var modeId in modeIds)
        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            cells.Add(new DeterministicBenchmarkCell(caseId, modeId, attempt, "migration-baseline"));
        }
    }

    private static IReadOnlyList<string> ReadInlineList(string line)
    {
        var start = line.IndexOf('[', StringComparison.Ordinal);
        var end = line.IndexOf(']', StringComparison.Ordinal);
        if (start < 0 || end <= start)
        {
            return Array.Empty<string>();
        }

        return line[(start + 1)..end]
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(value => value.Trim('"', '\''))
            .ToArray();
    }

    private static int ReadAttempts(IReadOnlyList<string> lines)
    {
        foreach (var rawLine in lines)
        {
            var trimmed = rawLine.Trim();
            if (!trimmed.StartsWith("attempts_per_case_mode:", StringComparison.Ordinal))
            {
                continue;
            }

            return int.TryParse(trimmed.Split(':', 2)[1].Trim(), out var parsed) ? parsed : 1;
        }

        return 1;
    }

    private static bool IsNextTopLevelSection(string rawLine)
    {
        return rawLine.Length > 0 && !char.IsWhiteSpace(rawLine[0]) && rawLine.Contains(':', StringComparison.Ordinal);
    }
}
