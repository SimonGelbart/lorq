using System.Globalization;

namespace Lorq.Adapters.Process;

/// <summary>
/// Deterministic no-LLM fixture consumed by the local file adapter runner.
/// </summary>
public sealed class DeterministicFakeAgentFixture
{
    private readonly IReadOnlyDictionary<string, DeterministicFakeAgentCell> cells;

    private DeterministicFakeAgentFixture(IReadOnlyDictionary<string, DeterministicFakeAgentCell> cells)
    {
        this.cells = cells;
    }

    public static DeterministicFakeAgentFixture Load(string fixturePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fixturePath);
        var path = Path.GetFullPath(fixturePath);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Fake agent fixture does not exist.", path);
        }

        return new DeterministicFakeAgentFixture(DeterministicFakeAgentFixtureParser.Parse(path));
    }

    public DeterministicFakeAgentCell Find(string caseId, string modeId, int attempt)
    {
        var key = DeterministicFakeAgentCell.Key(caseId, modeId, attempt);
        if (!cells.TryGetValue(key, out var cell))
        {
            throw new InvalidOperationException($"Fake agent fixture does not contain cell '{key}'.");
        }

        return cell;
    }
}

public sealed record DeterministicFakeAgentCell(
    string CaseId,
    string ModeId,
    int Attempt,
    string Status,
    int ElapsedMilliseconds,
    string FinalAnswer,
    int ExitCode,
    bool TimedOut,
    string? ErrorCategory,
    IReadOnlyDictionary<string, long> Usage,
    IReadOnlyList<IReadOnlyDictionary<string, string>> Events,
    IReadOnlyList<DeterministicFakeArtifact> Artifacts,
    IReadOnlyList<string> IntegrityWarnings)
{
    public static string Key(string caseId, string modeId, int attempt)
    {
        return string.Create(CultureInfo.InvariantCulture, $"{caseId}/{modeId}/{attempt}");
    }
}

public sealed record DeterministicFakeArtifact(string Path, string Kind);
