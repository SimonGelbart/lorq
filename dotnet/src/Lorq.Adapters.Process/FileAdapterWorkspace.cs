namespace Lorq.Adapters.Process;

/// <summary>
/// Workspace paths the adapter may read from or write to.
/// </summary>
public sealed record FileAdapterWorkspace(
    string Root,
    string EvidenceDirectory,
    string ArtifactsDirectory);
