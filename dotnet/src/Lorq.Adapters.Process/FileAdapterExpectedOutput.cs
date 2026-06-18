namespace Lorq.Adapters.Process;

/// <summary>
/// Files the adapter must produce for LORQ to accept the invocation.
/// </summary>
public sealed record FileAdapterExpectedOutput(string EvidencePath, string FinalAnswerPath);
