namespace Lorq.Adapters.Process;

/// <summary>
/// Process-level result captured by the adapter host.
/// </summary>
public sealed record FileAdapterProcessResult(int ExitCode, string StdoutPath, string StderrPath);
