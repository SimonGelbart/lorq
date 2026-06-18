namespace Lorq.Adapters.Process;

/// <summary>
/// Identity of the benchmark cell delegated to an adapter.
/// </summary>
public sealed record FileAdapterCell(
    string CellId,
    string CaseId,
    string ModeId,
    string AttemptId,
    string ShardId);
