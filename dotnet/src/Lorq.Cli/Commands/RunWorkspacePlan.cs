namespace Lorq.Cli.Commands;

internal sealed record RunWorkspacePlan(
    string CellId,
    string AttemptId,
    string CaseId,
    string ModeId,
    string CasePath,
    string ModePath,
    string RepositorySourceRoot,
    string WorkspaceRoot,
    string EvidenceDirectory,
    string ArtifactsDirectory,
    IReadOnlyList<RunMaterializationCopy> MaterializationCopies);
