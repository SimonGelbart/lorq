using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Lorq.Adapters.Process;

/// <summary>
/// In-process deterministic file adapter used to prove LORQ run orchestration without real LLM calls.
/// </summary>
public sealed class DeterministicFakeFileAdapter : IFileAdapter
{
    private readonly DeterministicFakeAgentFixture fixture;

    public DeterministicFakeFileAdapter(DeterministicFakeAgentFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        this.fixture = fixture;
    }

    public async ValueTask<FileAdapterEvidence> InvokeAsync(FileAdapterRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var attempt = ParseAttempt(request.Cell.AttemptId);
        var fixtureCell = fixture.Find(request.Cell.CaseId, request.Cell.ModeId, attempt);
        var finalAnswerPath = ResolvePath(request.Workspace.EvidenceDirectory, request.ExpectedOutput.FinalAnswerPath);
        var stdoutPath = Path.Combine(request.Workspace.EvidenceDirectory, "stdout.raw.txt");
        var stderrPath = Path.Combine(request.Workspace.EvidenceDirectory, "stderr.txt");

        Directory.CreateDirectory(request.Workspace.EvidenceDirectory);
        Directory.CreateDirectory(request.Workspace.ArtifactsDirectory);
        await File.WriteAllTextAsync(finalAnswerPath, fixtureCell.FinalAnswer, cancellationToken);
        await File.WriteAllTextAsync(stdoutPath, RenderStdout(fixtureCell), cancellationToken);
        await File.WriteAllTextAsync(stderrPath, string.Empty, cancellationToken);

        var evidence = EvidenceFrom(request, fixtureCell, stdoutPath, stderrPath, finalAnswerPath);
        var evidencePath = ResolvePath(request.Workspace.EvidenceDirectory, request.ExpectedOutput.EvidencePath);
        await File.WriteAllTextAsync(evidencePath, JsonSerializer.Serialize(evidence, FileAdapterJson.Options) + Environment.NewLine, cancellationToken);
        return evidence;
    }

    private static FileAdapterEvidence EvidenceFrom(
        FileAdapterRequest request,
        DeterministicFakeAgentCell fixtureCell,
        string stdoutPath,
        string stderrPath,
        string finalAnswerPath)
    {
        var toolCallCount = fixtureCell.Events.Count(item => item.TryGetValue("type", out var type) && type.StartsWith("tool.", StringComparison.Ordinal));
        return new FileAdapterEvidence(
            FileAdapterProtocol.EvidenceSchemaVersion,
            FileAdapterProtocol.ContractVersion,
            request.Cell.CellId,
            new FileAdapterDescriptor("deterministic-fake", "file-adapter", "v1alpha1", FileAdapterRuntimeMetadata.DeterministicFake()),
            fixtureCell.Status,
            new FileAdapterFinalAnswer(!string.IsNullOrWhiteSpace(fixtureCell.FinalAnswer), PortablePath(finalAnswerPath, request.Workspace.EvidenceDirectory), Summary(fixtureCell.FinalAnswer)),
            Usage(fixtureCell),
            new FileAdapterCounts(toolCallCount, fixtureCell.Artifacts.Count, fixtureCell.Events.Count),
            new FileAdapterTiming(fixtureCell.ElapsedMilliseconds, fixtureCell.TimedOut),
            new FileAdapterProcessResult(fixtureCell.ExitCode, PortablePath(stdoutPath, request.Workspace.EvidenceDirectory), PortablePath(stderrPath, request.Workspace.EvidenceDirectory)),
            TraceEvents(fixtureCell),
            Artifacts(fixtureCell),
            fixtureCell.IntegrityWarnings,
            Diagnostics(fixtureCell));
    }

    private static FileAdapterUsage Usage(DeterministicFakeAgentCell cell)
    {
        var input = UsageValue(cell, "input_tokens");
        var cached = UsageValue(cell, "cached_input_tokens");
        var output = UsageValue(cell, "output_tokens");
        return new FileAdapterUsage(input, cached, output, 0, 0m);
    }

    private static long UsageValue(DeterministicFakeAgentCell cell, string key)
    {
        return cell.Usage.TryGetValue(key, out var value) ? value : 0;
    }

    private static IReadOnlyList<FileAdapterTraceEvent> TraceEvents(DeterministicFakeAgentCell cell)
    {
        return cell.Events.Select(EventFrom).ToArray();
    }

    private static FileAdapterTraceEvent EventFrom(IReadOnlyDictionary<string, string> values)
    {
        var kind = values.GetValueOrDefault("type", "event");
        var message = values.GetValueOrDefault("command", values.GetValueOrDefault("text", kind));
        var path = ExtractPath(message);
        return new FileAdapterTraceEvent(kind, message, path);
    }

    private static IReadOnlyList<FileAdapterArtifact> Artifacts(DeterministicFakeAgentCell cell)
    {
        return cell.Artifacts.Select(ArtifactFrom).ToArray();
    }

    private static FileAdapterArtifact ArtifactFrom(DeterministicFakeArtifact artifact)
    {
        return new FileAdapterArtifact(artifact.Kind, artifact.Path, Sha256(artifact.Path));
    }

    private static IReadOnlyList<FileAdapterDiagnostic> Diagnostics(DeterministicFakeAgentCell cell)
    {
        if (cell.Status == "completed")
        {
            return Array.Empty<FileAdapterDiagnostic>();
        }

        var code = cell.ErrorCategory is null ? "LORQ-ADAPTER-STATUS" : "LORQ-ADAPTER-" + cell.ErrorCategory.Replace('_', '-').ToUpperInvariant();
        return new[] { new FileAdapterDiagnostic(code, "warning", $"Deterministic fixture emitted status '{cell.Status}'.") };
    }

    private static string RenderStdout(DeterministicFakeAgentCell cell)
    {
        var lines = cell.Events.Select(item => JsonSerializer.Serialize(item, FileAdapterJson.Options));
        return string.Join(Environment.NewLine, lines) + Environment.NewLine;
    }

    private static string Summary(string answer)
    {
        if (answer.Length <= 120)
        {
            return answer;
        }

        return answer[..120];
    }

    private static string? ExtractPath(string message)
    {
        const string readPrefix = "read ";
        return message.StartsWith(readPrefix, StringComparison.Ordinal) ? message[readPrefix.Length..] : null;
    }

    private static string Sha256(string text)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string ResolvePath(string evidenceDirectory, string path)
    {
        return Path.IsPathRooted(path) ? path : Path.Combine(evidenceDirectory, path);
    }

    private static string PortablePath(string path, string root)
    {
        return Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/');
    }

    private static int ParseAttempt(string attemptId)
    {
        const string prefix = "attempt-";
        if (attemptId.StartsWith(prefix, StringComparison.Ordinal) && int.TryParse(attemptId[prefix.Length..], out var parsed))
        {
            return parsed;
        }

        return 1;
    }
}
