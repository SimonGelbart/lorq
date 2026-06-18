namespace Lorq.Adapters.Process;

/// <summary>
/// Adapter diagnostic suitable for evidence ledgers and reports.
/// </summary>
public sealed record FileAdapterDiagnostic(string Code, string Severity, string Message);
