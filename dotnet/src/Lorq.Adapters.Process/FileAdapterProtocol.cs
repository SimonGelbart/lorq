namespace Lorq.Adapters.Process;

/// <summary>
/// Constants for the file-based one-shot adapter protocol.
/// </summary>
public static class FileAdapterProtocol
{
    public const string ContractVersion = "lorq.contract.v1alpha1";
    public const string RequestSchemaVersion = "lorq.file-adapter-request.v1alpha1";
    public const string EvidenceSchemaVersion = "lorq.file-adapter-evidence.v1alpha1";
    public const string RequestFileName = "adapter-request.json";
    public const string EvidenceFileName = "adapter-evidence.json";

    public static FileAdapterPaths PathsFor(string exchangeDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(exchangeDirectory);
        return FileAdapterPaths.FromDirectory(exchangeDirectory);
    }
}
