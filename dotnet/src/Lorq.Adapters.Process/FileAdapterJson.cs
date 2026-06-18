using System.Text.Json;

namespace Lorq.Adapters.Process;

/// <summary>
/// JSON settings for the one-shot file adapter protocol.
/// </summary>
public static class FileAdapterJson
{
    public static JsonSerializerOptions Options { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true,
    };
}
