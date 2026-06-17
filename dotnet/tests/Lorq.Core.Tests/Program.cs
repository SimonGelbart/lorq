using Lorq.Core;

var repoRoot = FindRepoRoot();
var goldenRoot = Path.Combine(repoRoot, "fixtures", "golden", "deterministic-orchestration");
var edgeRoot = Path.Combine(repoRoot, "fixtures", "conformance", "deterministic-orchestration", "edge-fixtures");

Run("validates merged golden experiment", () =>
{
    var result = LorqPackageValidator.Validate(Path.Combine(goldenRoot, "experiment-001"));
    AssertTrue(result.Ok, DescribePackage(result));
    AssertEqual("deterministic-benchmark", result.Package!.PackageId);
    AssertEqual("merged_experiment", result.Package.PackageKind);
    AssertEqual(8, result.Package.Cells.Count);
    AssertEqual(9, result.Package.ExpectedCellIds.Count);
    AssertEqual(1, result.Package.MissingCellIds.Count);
    AssertEqual(1, result.Package.Judgements.Count);
    AssertTrue(result.Package.Report is not null, "Expected report reference.");
});

Run("validates golden run shards", () =>
{
    foreach (var shardId in new[] { "shard-001", "shard-002" })
    {
        var result = LorqPackageValidator.Validate(Path.Combine(goldenRoot, shardId));
        AssertTrue(result.Ok, $"{shardId}: {DescribePackage(result)}");
        AssertEqual("run_shard", result.Package!.PackageKind);
        AssertEqual(shardId, result.Package.DeclaredShardIds.Single());
        AssertEqual(result.Package.Cells.Count, result.Package.ExpectedCellIds.Count);
    }
});

Run("rejects duplicate cell merge inputs with stable code", () =>
{
    var result = LorqPackageValidator.ValidateMergeInputs(new[]
    {
        Path.Combine(edgeRoot, "duplicate-cell-conflict", "shard-a"),
        Path.Combine(edgeRoot, "duplicate-cell-conflict", "shard-b"),
    });
    AssertTrue(!result.Ok, "Expected duplicate cell fixture to fail.");
    AssertTrue(result.DuplicateCellIds.Contains("duplicate-case__baseline__attempt-001"), "Expected duplicate cell id.");
    AssertTrue(result.Errors.Any(diagnostic => diagnostic.Code == "LORQ210"), DescribeMerge(result));
});

Run("rejects fingerprint mismatch merge inputs with stable code", () =>
{
    var result = LorqPackageValidator.ValidateMergeInputs(new[]
    {
        Path.Combine(edgeRoot, "fingerprint-mismatch", "shard-a"),
        Path.Combine(edgeRoot, "fingerprint-mismatch", "shard-b"),
    });
    AssertTrue(!result.Ok, "Expected fingerprint mismatch fixture to fail.");
    AssertTrue(result.FingerprintMismatch, "Expected fingerprint mismatch flag.");
    AssertTrue(result.Errors.Any(diagnostic => diagnostic.Code == "LORQ220"), DescribeMerge(result));
});

Console.WriteLine("Lorq.Core.Tests: all assertions passed");
return 0;

static void Run(string name, Action action)
{
    try
    {
        action();
        Console.WriteLine($"PASS {name}");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"FAIL {name}: {ex.Message}");
        Environment.Exit(1);
    }
}

static string FindRepoRoot()
{
    var current = AppContext.BaseDirectory;
    while (current is not null)
    {
        if (File.Exists(Path.Combine(current, "eval.config.yaml")) && Directory.Exists(Path.Combine(current, "fixtures")))
        {
            return current;
        }

        current = Directory.GetParent(current)?.FullName;
    }

    throw new InvalidOperationException("Could not find repo root from test output directory.");
}

static void AssertTrue(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static void AssertEqual<T>(T expected, T actual)
    where T : notnull
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"Expected {expected}, got {actual}.");
    }
}

static string DescribePackage(PackageValidationResult result)
{
    return string.Join("; ", result.Diagnostics.Select(diagnostic => $"{diagnostic.Code}:{diagnostic.Message}"));
}

static string DescribeMerge(MergeInputValidationResult result)
{
    return string.Join("; ", result.Diagnostics.Select(diagnostic => $"{diagnostic.Code}:{diagnostic.Message}"));
}
