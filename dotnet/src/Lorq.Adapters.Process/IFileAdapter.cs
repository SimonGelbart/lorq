namespace Lorq.Adapters.Process;

/// <summary>
/// Executes one LORQ file-adapter request and returns the full evidence contract.
/// </summary>
public interface IFileAdapter
{
    ValueTask<FileAdapterEvidence> InvokeAsync(FileAdapterRequest request, CancellationToken cancellationToken = default);
}
