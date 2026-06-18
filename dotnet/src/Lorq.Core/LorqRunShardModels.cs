using System.Text.Json.Nodes;

namespace Lorq.Core;

public sealed record LorqRunShardCellEvidence(
    string CellId,
    string CaseId,
    string ModeId,
    string AttemptId,
    string ShardId,
    string PromptText,
    string Category,
    string Status,
    bool FinalAnswerPresent,
    string FinalAnswer,
    int PromptChars,
    int FinalAnswerChars,
    JsonObject Adapter,
    JsonObject Usage,
    JsonObject Counts,
    JsonObject Timing,
    JsonArray Trace,
    JsonArray Artifacts,
    IReadOnlyList<string> IntegrityWarnings,
    JsonObject Process,
    JsonArray Diagnostics,
    string AdapterEvidenceJson);

public sealed record LorqRunShardWriteRequest(
    string PackageId,
    string ShardId,
    string OutputRoot,
    IReadOnlyList<LorqRunShardCellEvidence> Cells);

public sealed record LorqRunShardWriteResult(
    bool Ok,
    string PackageRoot,
    string PackageId,
    string ShardId,
    int CellCount,
    IReadOnlyList<LorqDiagnostic> Diagnostics)
{
    public IReadOnlyList<LorqDiagnostic> Errors => Diagnostics.Where(diagnostic => diagnostic.Severity == "error").ToArray();
}
