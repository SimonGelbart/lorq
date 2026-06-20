using Lorq.Adapters.Process;
using Lorq.Core;
using System.Text.Json;

namespace Lorq.Cli.Runtime;

internal sealed class RunCellExecutor
{
    private readonly RunPromptBuilder promptBuilder;
    private readonly RunCellEvidenceFactory evidenceFactory;

    public RunCellExecutor(RunPromptBuilder promptBuilder, RunCellEvidenceFactory evidenceFactory)
    {
        ArgumentNullException.ThrowIfNull(promptBuilder);
        ArgumentNullException.ThrowIfNull(evidenceFactory);
        this.promptBuilder = promptBuilder;
        this.evidenceFactory = evidenceFactory;
    }

    public async ValueTask<LorqRunShardCellEvidence> RunAsync(
        string shardId,
        IFileAdapter adapter,
        DeterministicBenchmarkCell cell,
        RunWorkspacePlan workspace,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shardId);
        ArgumentNullException.ThrowIfNull(adapter);
        ArgumentNullException.ThrowIfNull(cell);
        ArgumentNullException.ThrowIfNull(workspace);

        var promptText = promptBuilder.Build(workspace.CasePath);
        var request = CreateRequest(shardId, cell, workspace, promptText);
        await WriteRequestAsync(workspace.EvidenceDirectory, request, cancellationToken);
        var evidence = await adapter.InvokeAsync(request, cancellationToken);
        var evidenceJson = await ReadEvidenceJsonAsync(workspace.EvidenceDirectory, cancellationToken);
        return evidenceFactory.Create(shardId, cell, promptText, evidence, evidenceJson, workspace.EvidenceDirectory);
    }

    private static FileAdapterRequest CreateRequest(
        string shardId,
        DeterministicBenchmarkCell cell,
        RunWorkspacePlan workspace,
        string promptText)
    {
        return new FileAdapterRequest(
            FileAdapterProtocol.RequestSchemaVersion,
            FileAdapterProtocol.ContractVersion,
            new FileAdapterCell(workspace.CellId, cell.CaseId, cell.ModeId, workspace.AttemptId, shardId),
            new FileAdapterWorkspace(workspace.WorkspaceRoot, workspace.EvidenceDirectory, workspace.ArtifactsDirectory),
            new FileAdapterTask("prompt.txt", promptText),
            new FileAdapterLimits(30000),
            new FileAdapterExpectedOutput(FileAdapterProtocol.EvidenceFileName, "answer.md"));
    }

    private static async ValueTask WriteRequestAsync(
        string evidenceDirectory,
        FileAdapterRequest request,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(evidenceDirectory);
        var path = Path.Combine(evidenceDirectory, FileAdapterProtocol.RequestFileName);
        var json = JsonSerializer.Serialize(request, FileAdapterJson.Options) + Environment.NewLine;
        await File.WriteAllTextAsync(path, json, cancellationToken);
    }

    private static ValueTask<string> ReadEvidenceJsonAsync(string evidenceDirectory, CancellationToken cancellationToken)
    {
        var path = Path.Combine(evidenceDirectory, FileAdapterProtocol.EvidenceFileName);
        return new ValueTask<string>(File.ReadAllTextAsync(path, cancellationToken));
    }
}
