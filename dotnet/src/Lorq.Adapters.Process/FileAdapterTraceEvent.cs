namespace Lorq.Adapters.Process;

/// <summary>
/// Normalized trace event emitted by the adapter.
/// </summary>
public sealed record FileAdapterTraceEvent(string Kind, string Message, string? Path);
