namespace Lorq.Adapters.Process;

/// <summary>
/// Coarse execution counters emitted by an adapter.
/// </summary>
public sealed record FileAdapterCounts(int ToolCallCount, int ArtifactCount, int TraceEventCount);
