namespace Lorq.Adapters.Process;

/// <summary>
/// Adapter identity recorded in produced evidence.
/// </summary>
public sealed record FileAdapterDescriptor(string Id, string Kind, string Version);
