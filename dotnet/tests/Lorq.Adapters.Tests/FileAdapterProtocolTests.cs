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

    private static string SchemaConst(string fileName)
    {
        var path = Path.Combine(TestPaths.RepoRoot(), "schemas", fileName);
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.GetProperty("properties").GetProperty("schema_version").GetProperty("const").GetString()
            ?? throw new InvalidOperationException($"Schema {fileName} does not declare schema_version const.");
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
