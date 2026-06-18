using System.Text.Json;
using Lorq.Core;
using Lorq.Reporting;

var options = new JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
};

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: lorq validate-package <package-root> | validate-merge-inputs <shard-root> <shard-root> [...] | rebuild-indexes <package-root> <target-root> | merge-shards <shard-root> <shard-root> [...] --out <package-root> --package-id <id> [--benchmark <path>] [--allow-incompatible]");
    return 2;
}

switch (args[0])
{
    case "validate-package":
    {
        var result = LorqPackageValidator.Validate(args[1]);
        Console.WriteLine(JsonSerializer.Serialize(ValidationSummaryRenderer.FromPackageResult(result), options));
        return result.Ok ? 0 : 1;
    }

    case "validate-merge-inputs":
    {
        if (args.Length < 3)
        {
            Console.Error.WriteLine("validate-merge-inputs requires at least two shard roots.");
            return 2;
        }

        var result = LorqPackageValidator.ValidateMergeInputs(args.Skip(1));
        Console.WriteLine(JsonSerializer.Serialize(ValidationSummaryRenderer.FromMergeInputResult(result), options));
        return result.Ok ? 0 : 1;
    }

    case "rebuild-indexes":
    {
        if (args.Length < 3)
        {
            Console.Error.WriteLine("rebuild-indexes requires a source package root and target root.");
            return 2;
        }

        var result = LorqPackageIndexRebuilder.Rebuild(args[1], args[2]);
        Console.WriteLine(JsonSerializer.Serialize(ValidationSummaryRenderer.FromIndexRebuildResult(result), options));
        return result.Ok ? 0 : 1;
    }

    case "merge-shards":
    {
        var parsed = ParseMergeArgs(args.Skip(1).ToArray());
        if (parsed is null)
        {
            Console.Error.WriteLine("merge-shards requires at least one shard root plus --out <package-root> and --package-id <id>.");
            return 2;
        }

        var result = LorqPackageMerger.Merge(parsed);
        Console.WriteLine(JsonSerializer.Serialize(ValidationSummaryRenderer.FromPackageMergeResult(result), options));
        return result.Ok ? 0 : 1;
    }

    default:
        Console.Error.WriteLine($"Unknown command '{args[0]}'.");
        return 2;
}


static LorqPackageMergeRequest? ParseMergeArgs(IReadOnlyList<string> values)
{
    var shardRoots = new List<string>();
    string? outputRoot = null;
    string? packageId = null;
    string? benchmarkPath = null;
    var strict = true;

    for (var index = 0; index < values.Count; index++)
    {
        var value = values[index];
        switch (value)
        {
            case "--out" when index + 1 < values.Count:
                outputRoot = values[++index];
                break;
            case "--package-id" when index + 1 < values.Count:
                packageId = values[++index];
                break;
            case "--benchmark" when index + 1 < values.Count:
                benchmarkPath = values[++index];
                break;
            case "--allow-incompatible":
                strict = false;
                break;
            default:
                shardRoots.Add(value);
                break;
        }
    }

    if (shardRoots.Count == 0 || string.IsNullOrWhiteSpace(outputRoot) || string.IsNullOrWhiteSpace(packageId))
    {
        return null;
    }

    return new LorqPackageMergeRequest(shardRoots, outputRoot, packageId, benchmarkPath, strict);
}
