using System.Text.Json;
using Lorq.Adapters.Process;

if (args.Contains("--fail-without-evidence", StringComparer.Ordinal))
{
    return 42;
}

var requestPath = Environment.GetEnvironmentVariable("LORQ_ADAPTER_REQUEST") ?? throw new InvalidOperationException("LORQ_ADAPTER_REQUEST is required.");
var evidencePath = Environment.GetEnvironmentVariable("LORQ_ADAPTER_EVIDENCE") ?? throw new InvalidOperationException("LORQ_ADAPTER_EVIDENCE is required.");
var request = JsonSerializer.Deserialize<FileAdapterRequest>(File.ReadAllText(requestPath), FileAdapterJson.Options)
    ?? throw new InvalidOperationException("adapter-request.json could not be read.");

if (args.Contains("--sleep", StringComparer.Ordinal))
{
    await Task.Delay(TimeSpan.FromSeconds(30));
}

if (args.Contains("--write-malformed-evidence", StringComparer.Ordinal))
{
    Directory.CreateDirectory(request.Workspace.EvidenceDirectory);
    File.WriteAllText(evidencePath, "{ this is not valid json" + Environment.NewLine);
    return 0;
}

var assertCodexProfile = args.Contains("--assert-codex-profile", StringComparer.Ordinal);
if (assertCodexProfile && !HasCodexProfileEnvironment())
{
    return 43;
}

Directory.CreateDirectory(request.Workspace.EvidenceDirectory);
Directory.CreateDirectory(request.Workspace.ArtifactsDirectory);
var answerPath = Path.Combine(request.Workspace.EvidenceDirectory, request.ExpectedOutput.FinalAnswerPath);
var stdoutPath = Path.Combine(request.Workspace.EvidenceDirectory, "stdout.raw.txt");
var stderrPath = Path.Combine(request.Workspace.EvidenceDirectory, "stderr.txt");
File.WriteAllText(answerPath, "External one-shot adapter answer for " + request.Cell.CellId + Environment.NewLine);
File.WriteAllText(stdoutPath, "{\"type\":\"tool.command\",\"command\":\"external adapter\"}" + Environment.NewLine);
File.WriteAllText(stderrPath, string.Empty);

var adapterId = assertCodexProfile ? "codex-profile-test-adapter" : "external-test-adapter";
var evidence = CreateEvidence(request, adapterId, assertCodexProfile);
if (args.Contains("--write-no-final-answer", StringComparer.Ordinal))
{
    evidence = evidence with { FinalAnswer = null! };
}

if (args.Contains("--write-no-usage", StringComparer.Ordinal))
{
    evidence = evidence with { Usage = null! };
}

if (args.Contains("--write-missing-answer-file", StringComparer.Ordinal))
{
    File.Delete(answerPath);
}

File.WriteAllText(evidencePath, JsonSerializer.Serialize(evidence, FileAdapterJson.Options) + Environment.NewLine);
return 0;

static FileAdapterEvidence CreateEvidence(FileAdapterRequest request, string adapterId, bool assertCodexProfile)
{
    return new FileAdapterEvidence(
        FileAdapterProtocol.EvidenceSchemaVersion,
        FileAdapterProtocol.ContractVersion,
        request.Cell.CellId,
        new FileAdapterDescriptor(adapterId, "file-adapter", "v1alpha1"),
        "completed",
        new FileAdapterFinalAnswer(true, request.ExpectedOutput.FinalAnswerPath, "External one-shot adapter answer."),
        new FileAdapterUsage(11, 1, 13, 0, 0m),
        new FileAdapterCounts(1, 1, 1),
        new FileAdapterTiming(17, false),
        new FileAdapterProcessResult(0, "stdout.raw.txt", "stderr.txt"),
        new[] { new FileAdapterTraceEvent("tool.command", TraceMessage(assertCodexProfile), null) },
        new[] { new FileAdapterArtifact("answer", request.ExpectedOutput.FinalAnswerPath, "sha256-test") },
        Array.Empty<string>(),
        Array.Empty<FileAdapterDiagnostic>());
}

static bool HasCodexProfileEnvironment()
{
    return Environment.GetEnvironmentVariable("LORQ_ADAPTER_PROFILE") == "codex-cli"
        && !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LORQ_CODEX_COMMAND"))
        && (Environment.GetEnvironmentVariable("LORQ_CODEX_ARGUMENTS") ?? string.Empty).Contains("exec", StringComparison.Ordinal)
        && Environment.GetEnvironmentVariable("LORQ_CODEX_OUTPUT_FORMAT") == "codex-jsonl"
        && Environment.GetEnvironmentVariable("LORQ_CODEX_INVOCATION") == "one-shot-file-adapter";
}

static string TraceMessage(bool assertCodexProfile)
{
    return assertCodexProfile ? "external codex-profile adapter" : "external adapter";
}
