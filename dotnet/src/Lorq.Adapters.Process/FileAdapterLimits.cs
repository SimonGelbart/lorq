namespace Lorq.Adapters.Process;

/// <summary>
/// Deterministic execution limits for one adapter invocation.
/// </summary>
public sealed record FileAdapterLimits(int TimeoutMilliseconds);
