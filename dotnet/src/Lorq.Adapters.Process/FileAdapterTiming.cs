namespace Lorq.Adapters.Process;

/// <summary>
/// Adapter timing evidence for one invocation.
/// </summary>
public sealed record FileAdapterTiming(int ElapsedMilliseconds, bool TimedOut);
