namespace Lorq.Adapters.Process;

/// <summary>
/// Optional runtime metadata recorded by provider-specific adapter wrappers.
/// </summary>
public sealed record FileAdapterRuntimeMetadata(
    string Provider,
    string Runtime,
    string? RuntimeVersion,
    string? Profile,
    string? Command,
    string? PermissionProfile,
    string? OutputFormat,
    IReadOnlyDictionary<string, string> Extensions)
{
    public static FileAdapterRuntimeMetadata DeterministicFake()
    {
        return new FileAdapterRuntimeMetadata(
            "lorq",
            "deterministic-fake-file-adapter",
            "v1alpha1",
            "deterministic-fake",
            null,
            "no-token",
            FileAdapterProtocol.EvidenceSchemaVersion,
            new Dictionary<string, string>());
    }

    public static FileAdapterRuntimeMetadata CodexCli(
        string command,
        string outputFormat,
        string permissionProfile,
        IReadOnlyDictionary<string, string>? extensions = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputFormat);
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionProfile);
        return new FileAdapterRuntimeMetadata(
            "openai",
            "codex-cli",
            null,
            CodexFileAdapterProfile.Name,
            command,
            permissionProfile,
            outputFormat,
            extensions ?? new Dictionary<string, string>());
    }
}
