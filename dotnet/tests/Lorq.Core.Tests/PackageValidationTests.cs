using Lorq.Core;
using TUnit.Assertions;
using TUnit.Core;

namespace Lorq.Core.Tests;

public sealed class PackageValidationTests
{
    private readonly string goldenRoot = Path.Combine(TestPaths.RepoRoot(), "fixtures", "golden", "deterministic-orchestration");
    private readonly string edgeRoot = Path.Combine(TestPaths.RepoRoot(), "fixtures", "conformance", "deterministic-orchestration", "edge-fixtures");

    [Test]
    public async Task ValidatesMergedGoldenExperiment()
    {
        var result = LorqPackageValidator.Validate(Path.Combine(goldenRoot, "experiment-001"));

        await Assert.That(result.Ok).IsTrue().Because(DescribePackage(result));
        await Assert.That(result.Package).IsNotNull();
        await Assert.That(result.Package!.PackageId).IsEqualTo("deterministic-benchmark");
        await Assert.That(result.Package.PackageKind).IsEqualTo("merged_experiment");
        await Assert.That(result.Package.Cells.Count).IsEqualTo(8);
        await Assert.That(result.Package.ExpectedCellIds.Count).IsEqualTo(9);
        await Assert.That(result.Package.MissingCellIds.Count).IsEqualTo(1);
        await Assert.That(result.Package.Judgements.Count).IsEqualTo(1);
        await Assert.That(result.Package.Report).IsNotNull();
    }

    [Test]
    [Arguments("shard-001")]
    [Arguments("shard-002")]
    public async Task ValidatesGoldenRunShard(string shardId)
    {
        var result = LorqPackageValidator.Validate(Path.Combine(goldenRoot, shardId));

        await Assert.That(result.Ok).IsTrue().Because($"{shardId}: {DescribePackage(result)}");
        await Assert.That(result.Package).IsNotNull();
        await Assert.That(result.Package!.PackageKind).IsEqualTo("run_shard");
        await Assert.That(result.Package.DeclaredShardIds.Single()).IsEqualTo(shardId);
        await Assert.That(result.Package.Cells.Count).IsEqualTo(result.Package.ExpectedCellIds.Count);
    }

    [Test]
    public async Task RejectsDuplicateCellMergeInputsWithStableCode()
    {
        var result = LorqPackageValidator.ValidateMergeInputs(new[]
        {
            Path.Combine(edgeRoot, "duplicate-cell-conflict", "shard-a"),
            Path.Combine(edgeRoot, "duplicate-cell-conflict", "shard-b"),
        });

        await Assert.That(result.Ok).IsFalse();
        await Assert.That(result.DuplicateCellIds).Contains("duplicate-case__baseline__attempt-001");
        await Assert.That(result.Errors.Any(diagnostic => diagnostic.Code == "LORQ210")).IsTrue().Because(DescribeMerge(result));
    }

    [Test]
    public async Task RejectsFingerprintMismatchMergeInputsWithStableCode()
    {
        var result = LorqPackageValidator.ValidateMergeInputs(new[]
        {
            Path.Combine(edgeRoot, "fingerprint-mismatch", "shard-a"),
            Path.Combine(edgeRoot, "fingerprint-mismatch", "shard-b"),
        });

        await Assert.That(result.Ok).IsFalse();
        await Assert.That(result.FingerprintMismatch).IsTrue();
        await Assert.That(result.Errors.Any(diagnostic => diagnostic.Code == "LORQ220")).IsTrue().Because(DescribeMerge(result));
    }

    private static string DescribePackage(PackageValidationResult result)
    {
        return string.Join("; ", result.Diagnostics.Select(diagnostic => $"{diagnostic.Code}:{diagnostic.Message}"));
    }

    private static string DescribeMerge(MergeInputValidationResult result)
    {
        return string.Join("; ", result.Diagnostics.Select(diagnostic => $"{diagnostic.Code}:{diagnostic.Message}"));
    }
}
