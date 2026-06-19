using System.Text.Json;
using Lorq.Adapters.Process;
using TUnit.Assertions;
using TUnit.Core;

namespace Lorq.Adapters.Tests;

public sealed class FileAdapterProtocolTests
{
    [Test]
    public async Task ProtocolConstantsMatchCommittedSchemas()
    {
        await Assert.That(SchemaConst("lorq-file-adapter-request.v1alpha.schema.json")).IsEqualTo(FileAdapterProtocol.RequestSchemaVersion);
        await Assert.That(SchemaConst("lorq-file-adapter-evidence.v1alpha.schema.json")).IsEqualTo(FileAdapterProtocol.EvidenceSchemaVersion);
    }

    [Test]
    public async Task AdapterPathsUseCanonicalFileNames()
    {
        var paths = FileAdapterProtocol.PathsFor("exchange");

        await Assert.That(Path.GetFileName(paths.RequestPath)).IsEqualTo(FileAdapterProtocol.RequestFileName);
        await Assert.That(Path.GetFileName(paths.EvidencePath)).IsEqualTo(FileAdapterProtocol.EvidenceFileName);
    }

    [Test]
    public async Task RequestSerializesUsingCanonicalSnakeCaseFields()
    {
        var request = new FileAdapterRequest(
            FileAdapterProtocol.RequestSchemaVersion,
            FileAdapterProtocol.ContractVersion,
            new FileAdapterCell("case__mode__attempt-001", "case", "mode", "attempt-001", "shard-001"),
            new FileAdapterWorkspace(".", "evidence", "artifacts"),
            new FileAdapterTask("prompt.txt", "Explain the repository."),
            new FileAdapterLimits(30000),
            new FileAdapterExpectedOutput("adapter-evidence.json", "answer.md"));
        var json = JsonSerializer.Serialize(request, FileAdapterJson.Options);

        await Assert.That(json).Contains("schema_version");
        await Assert.That(json).Contains("expected_output");
        await Assert.That(json).Contains("timeout_milliseconds");
    }

    [Test]
    public async Task EvidenceContractRequiresMoreThanFinalAnswer()
    {
        var evidence = new FileAdapterEvidence(
            FileAdapterProtocol.EvidenceSchemaVersion,
            FileAdapterProtocol.ContractVersion,
            "case__mode__attempt-001",
            new FileAdapterDescriptor("deterministic-fake", "file-adapter", "v1"),
            "completed",
            new FileAdapterFinalAnswer(true, "answer.md", "Deterministic answer."),
            new FileAdapterUsage(10, 0, 20, 0, 0m),
            new FileAdapterCounts(1, 1, 1),
            new FileAdapterTiming(25, false),
            new FileAdapterProcessResult(0, "stdout.raw.txt", "stderr.txt"),
            new[] { new FileAdapterTraceEvent("tool", "read fixture", "fixtures/fake-agent.yaml") },
            new[] { new FileAdapterArtifact("answer", "answer.md", "sha256-placeholder") },
            Array.Empty<string>(),
            Array.Empty<FileAdapterDiagnostic>());
        var json = JsonSerializer.Serialize(evidence, FileAdapterJson.Options);

        await Assert.That(json).Contains("final_answer");
        await Assert.That(json).Contains("usage");
        await Assert.That(json).Contains("timing");
        await Assert.That(json).Contains("process");
        await Assert.That(json).Contains("trace");
        await Assert.That(json).Contains("artifacts");
    }

    [Test]
    public async Task DeterministicFakeAdapterWritesFullEvidenceFile()
    {
        using var workspace = TemporaryDirectory.Create();
        var fixture = DeterministicFakeAgentFixture.Load(Path.Combine(TestPaths.RepoRoot(), "fixtures", "conformance", "deterministic-orchestration", "fixtures", "fake-agent.yaml"));
        var adapter = new DeterministicFakeFileAdapter(fixture);
        var request = new FileAdapterRequest(
            FileAdapterProtocol.RequestSchemaVersion,
            FileAdapterProtocol.ContractVersion,
            new FileAdapterCell("successful-comparison__baseline__attempt-001", "successful-comparison", "baseline", "attempt-001", "shard-001"),
            new FileAdapterWorkspace(workspace.Path, workspace.Path, Path.Combine(workspace.Path, "artifacts")),
            new FileAdapterTask("prompt.txt", "Explain deterministic evidence."),
            new FileAdapterLimits(30000),
            new FileAdapterExpectedOutput(FileAdapterProtocol.EvidenceFileName, "answer.md"));

        var evidence = await adapter.InvokeAsync(request);

        await Assert.That(evidence.Status).IsEqualTo("completed");
        await Assert.That(evidence.FinalAnswer.Present).IsTrue();
        await Assert.That(File.Exists(Path.Combine(workspace.Path, FileAdapterProtocol.EvidenceFileName))).IsTrue();
        await Assert.That(File.ReadAllText(Path.Combine(workspace.Path, FileAdapterProtocol.EvidenceFileName))).Contains("usage");
        await Assert.That(File.ReadAllText(Path.Combine(workspace.Path, FileAdapterProtocol.EvidenceFileName))).Contains("trace");
    }

    [Test]
    public async Task ExternalProcessAdapterReadsEvidenceFromOneShotProtocol()
    {
        using var workspace = TemporaryDirectory.Create();
        var request = AdapterRequest(workspace.Path, "external-case__baseline__attempt-001");
        var adapter = new ExternalFileAdapterProcess(new FileAdapterProcessCommand(DotnetExecutable(), new[] { TestHostDll() }, TestPaths.RepoRoot(), new Dictionary<string, string>()));

        var evidence = await adapter.InvokeAsync(request);

        await Assert.That(evidence.Adapter.Id).IsEqualTo("external-test-adapter");
        await Assert.That(evidence.CellId).IsEqualTo(request.Cell.CellId);
        await Assert.That(evidence.FinalAnswer.Present).IsTrue();
        await Assert.That(File.Exists(Path.Combine(workspace.Path, FileAdapterProtocol.RequestFileName))).IsTrue();
        await Assert.That(File.Exists(Path.Combine(workspace.Path, FileAdapterProtocol.EvidenceFileName))).IsTrue();
        await Assert.That(File.Exists(Path.Combine(workspace.Path, "adapter-process.stdout.txt"))).IsTrue();
        await Assert.That(File.Exists(Path.Combine(workspace.Path, "adapter-process.stderr.txt"))).IsTrue();
    }

    [Test]
    public async Task CodexProfileAddsWrapperEnvironmentWithoutLaunchingCodex()
    {
        var command = FileAdapterProcessCommand.Create("lorq-codex-wrapper");

        var profiled = CodexFileAdapterProfile.ApplyTo(command, "codex-test", new[] { "exec", "--json", "--model", "test-model" });

        await Assert.That(profiled.Executable).IsEqualTo("lorq-codex-wrapper");
        await Assert.That(profiled.EnvironmentVariables["LORQ_ADAPTER_PROFILE"]).IsEqualTo("codex-cli");
        await Assert.That(profiled.EnvironmentVariables["LORQ_CODEX_COMMAND"]).IsEqualTo("codex-test");
        await Assert.That(profiled.EnvironmentVariables["LORQ_CODEX_ARGUMENTS"]).Contains("--json");
        await Assert.That(profiled.EnvironmentVariables["LORQ_CODEX_OUTPUT_FORMAT"]).IsEqualTo("codex-jsonl");
    }

    [Test]
    public async Task ExternalProcessAdapterPassesCodexProfileEnvironmentToWrapper()
    {
        using var workspace = TemporaryDirectory.Create();
        var request = AdapterRequest(workspace.Path, "codex-profile__baseline__attempt-001");
        var command = new FileAdapterProcessCommand(DotnetExecutable(), new[] { TestHostDll(), "--assert-codex-profile" }, TestPaths.RepoRoot(), new Dictionary<string, string>());
        var adapter = new ExternalFileAdapterProcess(CodexFileAdapterProfile.ApplyTo(command, "codex-test", new[] { "exec", "--json" }));

        var evidence = await adapter.InvokeAsync(request);

        await Assert.That(evidence.Adapter.Id).IsEqualTo("codex-profile-test-adapter");
        await Assert.That(evidence.Trace[0].Message).Contains("codex-profile");
    }

    [Test]
    public async Task ExternalProcessAdapterFailsWhenEvidenceIsMissing()
    {
        using var workspace = TemporaryDirectory.Create();
        var request = AdapterRequest(workspace.Path, "missing-evidence__baseline__attempt-001");
        var adapter = new ExternalFileAdapterProcess(new FileAdapterProcessCommand(DotnetExecutable(), new[] { TestHostDll(), "--fail-without-evidence" }, TestPaths.RepoRoot(), new Dictionary<string, string>()));

        var exception = await Assert.ThrowsAsync<FileAdapterProtocolException>(() => adapter.InvokeAsync(request).AsTask());

        await Assert.That(exception!.Code).IsEqualTo("LORQ-ADAPTER-EVIDENCE-MISSING");
    }

    private static string SchemaConst(string fileName)
    {
        var path = Path.Combine(TestPaths.RepoRoot(), "schemas", fileName);
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.GetProperty("properties").GetProperty("schema_version").GetProperty("const").GetString()
            ?? throw new InvalidOperationException($"Schema {fileName} does not declare schema_version const.");
    }

    private static FileAdapterRequest AdapterRequest(string workspace, string cellId)
    {
        return new FileAdapterRequest(
            FileAdapterProtocol.RequestSchemaVersion,
            FileAdapterProtocol.ContractVersion,
            new FileAdapterCell(cellId, "external-case", "baseline", "attempt-001", "shard-001"),
            new FileAdapterWorkspace(TestPaths.RepoRoot(), workspace, Path.Combine(workspace, "artifacts")),
            new FileAdapterTask("prompt.txt", "Explain deterministic evidence."),
            new FileAdapterLimits(30000),
            new FileAdapterExpectedOutput(FileAdapterProtocol.EvidenceFileName, "answer.md"));
    }

    private static string TestHostDll()
    {
        var root = Path.Combine(TestPaths.RepoRoot(), "dotnet", "tests", "Lorq.Adapter.TestHost", "bin");
        if (!Directory.Exists(root))
        {
            throw new DirectoryNotFoundException(root);
        }

        var candidates = Directory
            .EnumerateFiles(root, "Lorq.Adapter.TestHost.dll", SearchOption.AllDirectories)
            .Where(path => path.Contains($"{Path.DirectorySeparatorChar}net10.0{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .OrderByDescending(path => path.Contains($"{Path.DirectorySeparatorChar}Release{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .ThenBy(path => path, StringComparer.Ordinal)
            .ToArray();

        return candidates.Length > 0
            ? candidates[0]
            : throw new FileNotFoundException("Lorq.Adapter.TestHost.dll was not found under " + root);
    }

    private static string DotnetExecutable()
    {
        return Environment.GetEnvironmentVariable("DOTNET_ROOT") is { Length: > 0 } dotnetRoot
            ? Path.Combine(dotnetRoot, "dotnet")
            : "dotnet";
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public string Path { get; }

        private TemporaryDirectory(string path)
        {
            Path = path;
        }

        public static TemporaryDirectory Create()
        {
            return new TemporaryDirectory(Directory.CreateTempSubdirectory("lorq-adapter-test-").FullName);
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
