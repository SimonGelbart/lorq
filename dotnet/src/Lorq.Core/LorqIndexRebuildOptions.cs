using System.Text.Json.Nodes;

namespace Lorq.Core;

public sealed record LorqIndexRebuildOptions(
    IReadOnlyList<string>? ExpectedCellIds = null,
    IReadOnlyList<JsonObject>? SourceShardWarnings = null)
{
    public static LorqIndexRebuildOptions Default { get; } = new();
}
