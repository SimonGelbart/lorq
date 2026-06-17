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
    Console.Error.WriteLine("Usage: lorq validate-package <package-root> | validate-merge-inputs <shard-root> <shard-root> [...] | rebuild-indexes <package-root> <target-root>");
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

    default:
        Console.Error.WriteLine($"Unknown command '{args[0]}'.");
        return 2;
}
