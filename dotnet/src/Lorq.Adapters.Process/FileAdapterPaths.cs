namespace Lorq.Adapters.Process;

/// <summary>
/// Canonical request and evidence locations for one adapter exchange.
/// </summary>
public sealed record FileAdapterPaths(string ExchangeDirectory, string RequestPath, string EvidencePath)
{
    public static FileAdapterPaths FromDirectory(string exchangeDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(exchangeDirectory);
        var fullPath = Path.GetFullPath(exchangeDirectory);
        return new FileAdapterPaths(
            fullPath,
            Path.Combine(fullPath, FileAdapterProtocol.RequestFileName),
            Path.Combine(fullPath, FileAdapterProtocol.EvidenceFileName));
    }
}
