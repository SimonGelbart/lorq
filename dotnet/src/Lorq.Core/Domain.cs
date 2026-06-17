namespace Lorq.Core;

public sealed record LorqDiagnostic(
    string Code,
    string Severity,
    string Message,
    string? Path = null);

public sealed record RunShard(
    string ShardId,
    int CellCount,
    IReadOnlyList<string> CellIds);

public sealed record RunCell(
    string CellId,
    string CaseId,
    string ModeId,
    string AttemptId,
    string ShardId,
    string Status,
    bool FinalAnswerPresent,
    string EvidencePath);

public sealed record JudgementPass(
    string Name,
    string Backend,
    bool RealLlmUsed,
    int CellCount,
    int JudgedCellCount);

public sealed record ReportReference(
    string PrimaryJudgement,
    string JsonPath,
    string MarkdownPath,
    int CaseCount);

public sealed record ExperimentPackage(
    string RootPath,
    string PackageId,
    string PackageKind,
    int PackageSchemaVersion,
    IReadOnlyList<string> DeclaredShardIds,
    IReadOnlyList<RunShard> RunShards,
    IReadOnlyList<RunCell> Cells,
    IReadOnlyList<string> ExpectedCellIds,
    IReadOnlyList<string> MissingCellIds,
    IReadOnlyList<JudgementPass> Judgements,
    ReportReference? Report,
    int IntegrityWarningCount,
    bool IntegrityOk);

public sealed record PackageValidationResult(
    bool Ok,
    ExperimentPackage? Package,
    IReadOnlyList<LorqDiagnostic> Diagnostics)
{
    public IReadOnlyList<LorqDiagnostic> Errors => Diagnostics.Where(d => d.Severity == "error").ToArray();
    public IReadOnlyList<LorqDiagnostic> Warnings => Diagnostics.Where(d => d.Severity == "warning").ToArray();
}

public sealed record MergeInputValidationResult(
    bool Ok,
    IReadOnlyList<LorqDiagnostic> Diagnostics,
    IReadOnlyList<string> CellIds,
    IReadOnlyList<string> DuplicateCellIds,
    bool FingerprintMismatch)
{
    public IReadOnlyList<LorqDiagnostic> Errors => Diagnostics.Where(d => d.Severity == "error").ToArray();
}
