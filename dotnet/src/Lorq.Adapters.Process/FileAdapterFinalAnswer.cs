namespace Lorq.Adapters.Process;

/// <summary>
/// Final answer reference and optional inline summary.
/// </summary>
public sealed record FileAdapterFinalAnswer(bool Present, string Path, string? Summary);
