namespace Lorq.Adapters.Process;

/// <summary>
/// Artifact reference produced by the adapter.
/// </summary>
public sealed record FileAdapterArtifact(string Kind, string Path, string Sha256);
