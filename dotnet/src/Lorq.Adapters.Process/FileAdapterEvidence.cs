namespace Lorq.Adapters.Process;

/// <summary>
/// Full adapter evidence contract produced by a one-shot adapter.
/// </summary>
public sealed record FileAdapterEvidence(
    string SchemaVersion,
    string ContractVersion,
    string CellId,
    FileAdapterDescriptor Adapter,
    string Status,
    FileAdapterFinalAnswer FinalAnswer,
    FileAdapterUsage Usage,
    FileAdapterCounts Counts,
    FileAdapterTiming Timing,
    FileAdapterProcessResult Process,
    IReadOnlyList<FileAdapterTraceEvent> Trace,
    IReadOnlyList<FileAdapterArtifact> Artifacts,
    IReadOnlyList<string> IntegrityWarnings,
    IReadOnlyList<FileAdapterDiagnostic> Diagnostics);
