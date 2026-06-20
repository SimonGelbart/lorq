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
        IReadOnlyList<ShardId> declaredShards,
        IReadOnlyList<RunShard> runShards,
        IReadOnlyList<CellId> presentCells)
    {
        ValidateDeclaredShards(declaredShards, runShards);
        ValidateShardCells(runShards, presentCells);
    }

    private RunShard ReadManifest(string manifestPath)
    {
        using var document = JsonHelpers.ReadDocument(manifestPath);
        var shardId = new ShardId(JsonHelpers.RequiredString(document.RootElement, "shard_id", manifestPath));
        var cellIds = JsonHelpers.OptionalStringArray(document.RootElement, "cell_ids").Select(cellId => new CellId(cellId)).ToArray();
        var declaredCount = JsonHelpers.OptionalInt(document.RootElement, "cell_count", -1);
        if (declaredCount != cellIds.Length)
        {
            diagnostics.Error("LORQ081", $"Shard manifest cell_count {declaredCount} does not match cell_ids count {cellIds.Length}.", manifestPath);
        }

        return new RunShard(shardId.Value, declaredCount, cellIds.Select(cellId => cellId.Value).ToArray());
    }

    private void ValidateDeclaredShards(IReadOnlyList<ShardId> declaredShards, IReadOnlyList<RunShard> runShards)
    {
        var manifestShardIds = runShards.Select(shard => new ShardId(shard.ShardId)).OrderBy(shardId => shardId.Value, StringComparer.Ordinal).ToArray();
        var declaredShardIds = declaredShards.OrderBy(shardId => shardId.Value, StringComparer.Ordinal).ToArray();
        if (!declaredShardIds.SequenceEqual(manifestShardIds))
        {
            diagnostics.Error("LORQ110", "experiment.yaml shards do not match run shard manifests.");
        }
    }

    private void ValidateShardCells(IReadOnlyList<RunShard> runShards, IReadOnlyList<CellId> presentCells)
    {
        var manifestCells = runShards
            .SelectMany(shard => shard.CellIds)
            .Select(cellId => new CellId(cellId))
            .OrderBy(cellId => cellId.Value, StringComparer.Ordinal)
            .ToArray();
        var coverageCells = presentCells.OrderBy(cellId => cellId.Value, StringComparer.Ordinal).ToArray();
        if (!manifestCells.SequenceEqual(coverageCells))
        {
            diagnostics.Error("LORQ111", "Run shard manifest cells do not match coverage present_cell_ids.");
        }
    }
}
