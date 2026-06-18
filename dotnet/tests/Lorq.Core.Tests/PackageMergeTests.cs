using Lorq.Core;
using TUnit.Assertions;
using TUnit.Core;

namespace Lorq.Core.Tests;

public sealed class PackageMergeTests
{
    private readonly string goldenRoot = Path.Combine(TestPaths.RepoRoot(), "fixtures", "golden", "deterministic-orchestration");
    private readonly string edgeRoot = Path.Combine(TestPaths.RepoRoot(), "fixtures", "conformance", "deterministic-orchestration", "edge-fixtures");
    private readonly string benchmarkPath = Path.Combine(TestPaths.RepoRoot(), "fixtures", "conformance", "deterministic-orchestration", "benchmark.yaml");

    [Test]
    public async Task MergesGoldenRunShardsIntoValidatedExperimentPackage()
    {
        using var workspace = TemporaryDirectory.Create();
        CopyDirectory(Path.Combine(goldenRoot, "shard-001"), Path.Combine(workspace.Path, "shard-001"));
        CopyDirectory(Path.Combine(goldenRoot, "shard-002"), Path.Combine(workspace.Path, "shard-002"));

        var targetRoot = Path.Combine(workspace.Path, "experiment-001");
        var result = LorqPackageMerger.Merge(
            new[]
            {
                Path.Combine(workspace.Path, "shard-001"),
                Path.Combine(workspace.Path, "shard-002"),
            },
            targetRoot,
            "deterministic-benchmark",
            benchmarkPath);
        var validation = LorqPackageValidator.Validate(targetRoot);

        await Assert.That(result.Ok).IsTrue().Because(Describe(result));
        await Assert.That(result.CellCount).IsEqualTo(8);
        await Assert.That(result.ExpectedCellCount).IsEqualTo(9);
        await Assert.That(result.MissingCellIds).Contains("skipped-coverage__graphify-plus__attempt-001");
        await Assert.That(validation.Ok).IsTrue().Because(DescribePackage(validation));
        await Assert.That(validation.Package!.Report).IsNull();
    }

    [Test]
    public async Task MergedGoldenCoreIndexesMatchPythonBaselineBytes()
    {
        using var workspace = TemporaryDirectory.Create();
        CopyDirectory(Path.Combine(goldenRoot, "shard-001"), Path.Combine(workspace.Path, "shard-001"));
        CopyDirectory(Path.Combine(goldenRoot, "shard-002"), Path.Combine(workspace.Path, "shard-002"));

        var targetRoot = Path.Combine(workspace.Path, "experiment-001");
        var result = LorqPackageMerger.Merge(
            new[]
            {
                Path.Combine(workspace.Path, "shard-001"),
                Path.Combine(workspace.Path, "shard-002"),
            },
            targetRoot,
            "deterministic-benchmark",
            benchmarkPath);

        await Assert.That(result.Ok).IsTrue().Because(Describe(result));
        foreach (var relativePath in CoreIndexPaths())
        {
            await AssertFileBytes(relativePath, targetRoot);
        }
    }

    [Test]
    public async Task RejectsDuplicateCellConflictByDefault()
    {
        using var workspace = TemporaryDirectory.Create();

        var result = LorqPackageMerger.Merge(
            new[]
            {
                Path.Combine(edgeRoot, "duplicate-cell-conflict", "shard-a"),
                Path.Combine(edgeRoot, "duplicate-cell-conflict", "shard-b"),
            },
            Path.Combine(workspace.Path, "experiment-001"),
            "duplicate-cell-conflict");

        await Assert.That(result.Ok).IsFalse();
        await Assert.That(result.DuplicateCellIds).Contains("duplicate-case__baseline__attempt-001");
        await Assert.That(result.Errors.Any(diagnostic => diagnostic.Code == "LORQ210")).IsTrue().Because(Describe(result));
    }

    [Test]
    public async Task RejectsFingerprintMismatchByDefault()
    {
        using var workspace = TemporaryDirectory.Create();

        var result = LorqPackageMerger.Merge(
            new[]
            {
                Path.Combine(edgeRoot, "fingerprint-mismatch", "shard-a"),
                Path.Combine(edgeRoot, "fingerprint-mismatch", "shard-b"),
            },
            Path.Combine(workspace.Path, "experiment-001"),
            "fingerprint-mismatch");

        await Assert.That(result.Ok).IsFalse();
        await Assert.That(result.FingerprintMismatch).IsTrue();
        await Assert.That(result.Errors.Any(diagnostic => diagnostic.Code == "LORQ220")).IsTrue().Because(Describe(result));
    }

    private static IReadOnlyList<string> CoreIndexPaths()
    {
        return new[]
        {
            ".lorq/coverage.json",
            ".lorq/fingerprints.json",
            ".lorq/integrity.json",
            ".lorq/merge-log.json",
            ".lorq/cells/no-final-answer__baseline__attempt-001.json",
            ".lorq/cells/no-final-answer__graphify-plus__attempt-001.json",
            ".lorq/cells/no-final-answer__graphify__attempt-001.json",
            ".lorq/cells/skipped-coverage__baseline__attempt-001.json",
            ".lorq/cells/skipped-coverage__graphify__attempt-001.json",
            ".lorq/cells/successful-comparison__baseline__attempt-001.json",
            ".lorq/cells/successful-comparison__graphify-plus__attempt-001.json",
            ".lorq/cells/successful-comparison__graphify__attempt-001.json",
        };
    }

    private async Task AssertFileBytes(string relativePath, string targetRoot)
    {
        var expected = Path.Combine(goldenRoot, "experiment-001", relativePath.Replace('/', Path.DirectorySeparatorChar));
        var actual = Path.Combine(targetRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        await Assert.That(File.Exists(actual)).IsTrue().Because(actual);
        await Assert.That(await File.ReadAllTextAsync(actual)).IsEqualTo(await File.ReadAllTextAsync(expected)).Because(relativePath);
    }

    private static string Describe(LorqPackageMergeResult result)
    {
        return string.Join("; ", result.Diagnostics.Select(diagnostic => $"{diagnostic.Code}:{diagnostic.Message}"));
    }

    private static string DescribePackage(PackageValidationResult result)
    {
        return string.Join("; ", result.Diagnostics.Select(diagnostic => $"{diagnostic.Code}:{diagnostic.Message}"));
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(directory.Replace(source, destination, StringComparison.Ordinal));
        }

        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            File.Copy(file, file.Replace(source, destination, StringComparison.Ordinal), overwrite: true);
        }
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public string Path { get; }

        private TemporaryDirectory(string path)
        {
            Path = path;
        }

        public static TemporaryDirectory Create()
        {
            return new TemporaryDirectory(Directory.CreateTempSubdirectory("lorq-package-merge-").FullName);
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
