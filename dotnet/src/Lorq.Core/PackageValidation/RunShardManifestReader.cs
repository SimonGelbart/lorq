namespace Lorq.Core.PackageValidation;

internal sealed class RunShardManifestReader
{
    private readonly PackageValidationDiagnostics diagnostics;

    public RunShardManifestReader(PackageValidationDiagnostics diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        this.diagnostics = diagnostics;
    }

    public IReadOnlyList<RunShard> Read(string root)
    {
        var runsRoot = Path.Combine(root, "runs");
        if (!Directory.Exists(runsRoot))
        {
            diagnostics.Error("LORQ080", "Package is missing runs directory.", runsRoot);
            return Array.Empty<RunShard>();
        }

        var shards = new List<RunShard>();
        foreach (var manifestPath in Directory.EnumerateFiles(runsRoot, "shard.manifest.json", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            shards.Add(ReadManifest(manifestPath));
        }

        return shards;
    }

    public void ValidateShardReferences(
        IReadOnlyList<string> declaredShards,
        IReadOnlyList<RunShard> runShards,
        IReadOnlyList<string> presentCells)
    {
        ValidateDeclaredShards(declaredShards, runShards);
        ValidateShardCells(runShards, presentCells);
    }

    private RunShard ReadManifest(string manifestPath)
    {
        using var document = JsonHelpers.ReadDocument(manifestPath);
        var shardId = JsonHelpers.RequiredString(document.RootElement, "shard_id", manifestPath);
        var cellIds = JsonHelpers.OptionalStringArray(document.RootElement, "cell_ids");
        var declaredCount = JsonHelpers.OptionalInt(document.RootElement, "cell_count", -1);
        if (declaredCount != cellIds.Count)
        {
            diagnostics.Error("LORQ081", $"Shard manifest cell_count {declaredCount} does not match cell_ids count {cellIds.Count}.", manifestPath);
        }

        return new RunShard(shardId, declaredCount, cellIds);
    }

    private void ValidateDeclaredShards(IReadOnlyList<string> declaredShards, IReadOnlyList<RunShard> runShards)
    {
        var manifestShardIds = runShards.Select(shard => shard.ShardId).Order(StringComparer.Ordinal).ToArray();
        var declaredShardIds = declaredShards.Order(StringComparer.Ordinal).ToArray();
        if (!declaredShardIds.SequenceEqual(manifestShardIds, StringComparer.Ordinal))
        {
            diagnostics.Error("LORQ110", "experiment.yaml shards do not match run shard manifests.");
        }
    }

    private void ValidateShardCells(IReadOnlyList<RunShard> runShards, IReadOnlyList<string> presentCells)
    {
        var manifestCells = runShards.SelectMany(shard => shard.CellIds).Order(StringComparer.Ordinal).ToArray();
        var coverageCells = presentCells.Order(StringComparer.Ordinal).ToArray();
        if (!manifestCells.SequenceEqual(coverageCells, StringComparer.Ordinal))
        {
            diagnostics.Error("LORQ111", "Run shard manifest cells do not match coverage present_cell_ids.");
        }
    }
}
