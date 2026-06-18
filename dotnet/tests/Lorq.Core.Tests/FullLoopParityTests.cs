using Lorq.Core;
using Lorq.Reporting;
using TUnit.Assertions;
using TUnit.Core;

namespace Lorq.Core.Tests;

public sealed class FullLoopParityTests
{
    private readonly string goldenRoot = Path.Combine(TestPaths.RepoRoot(), "fixtures", "golden", "deterministic-orchestration");
    private readonly string benchmarkPath = Path.Combine(TestPaths.RepoRoot(), "fixtures", "conformance", "deterministic-orchestration", "benchmark.yaml");
    private readonly string fixturePath = Path.Combine(TestPaths.RepoRoot(), "fixtures", "conformance", "deterministic-orchestration", "fixtures", "fake-judge.yaml");

    [Test]
    public async Task DotNetFullLoopMatchesFrozenPythonGoldenPackage()
    {
        using var workspace = TemporaryDirectory.Create();
        CopyDirectory(Path.Combine(goldenRoot, "shard-001"), Path.Combine(workspace.Path, "shard-001"));
        CopyDirectory(Path.Combine(goldenRoot, "shard-002"), Path.Combine(workspace.Path, "shard-002"));

        var experimentRoot = Path.Combine(workspace.Path, "experiment-001");
        var merge = LorqPackageMerger.Merge(
            new[]
            {
                Path.Combine(workspace.Path, "shard-001"),
                Path.Combine(workspace.Path, "shard-002"),
            },
            experimentRoot,
            "deterministic-benchmark",
            benchmarkPath);
        var judgement = LorqDeterministicPackageJudge.Attach(experimentRoot, "judge-primary", fixturePath);
        var report = LorqPackageReportRenderer.Render(experimentRoot, "judge-primary");
        var validation = LorqPackageValidator.Validate(experimentRoot);

        await Assert.That(merge.Ok).IsTrue().Because(Describe(merge));
        await Assert.That(judgement.Ok).IsTrue().Because(Describe(judgement));
        await Assert.That(report.Ok).IsTrue().Because(Describe(report));
        await Assert.That(validation.Ok).IsTrue().Because(Describe(validation));
        await AssertPackageBytes(experimentRoot);
    }

    [Test]
    public async Task DotNetFullLoopPreservesExpectedCoverageGap()
    {
        using var workspace = TemporaryDirectory.Create();
        CopyDirectory(Path.Combine(goldenRoot, "shard-001"), Path.Combine(workspace.Path, "shard-001"));
        CopyDirectory(Path.Combine(goldenRoot, "shard-002"), Path.Combine(workspace.Path, "shard-002"));

        var experimentRoot = Path.Combine(workspace.Path, "experiment-001");
        var merge = LorqPackageMerger.Merge(
            new[]
            {
                Path.Combine(workspace.Path, "shard-001"),
                Path.Combine(workspace.Path, "shard-002"),
            },
            experimentRoot,
            "deterministic-benchmark",
            benchmarkPath);
        var judgement = LorqDeterministicPackageJudge.Attach(experimentRoot, "judge-primary", fixturePath);
        var report = LorqPackageReportRenderer.Render(experimentRoot, "judge-primary");

        await Assert.That(merge.ExpectedCellCount).IsEqualTo(9);
        await Assert.That(merge.CellCount).IsEqualTo(8);
        await Assert.That(merge.MissingCellIds).Contains("skipped-coverage__graphify-plus__attempt-001");
        await Assert.That(judgement.MissingExpectedCellIds).Contains("skipped-coverage__graphify-plus__attempt-001");
        await Assert.That(report.MissingExpectedCellIds).Contains("skipped-coverage__graphify-plus__attempt-001");
    }

    private async Task AssertPackageBytes(string experimentRoot)
    {
        var expectedRoot = Path.Combine(goldenRoot, "experiment-001");
        var expectedFiles = RelativeFiles(expectedRoot);
        var actualFiles = RelativeFiles(experimentRoot);

        await Assert.That(actualFiles).IsEquivalentTo(expectedFiles);
        foreach (var relativePath in expectedFiles)
        {
            var expected = Path.Combine(expectedRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            var actual = Path.Combine(experimentRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            await Assert.That(await File.ReadAllTextAsync(actual)).IsEqualTo(await File.ReadAllTextAsync(expected)).Because(relativePath);
        }
    }

    private static IReadOnlyList<string> RelativeFiles(string root)
    {
        return Directory
            .EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/'))
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    private static string Describe(LorqPackageMergeResult result)
    {
        return string.Join("; ", result.Diagnostics.Select(diagnostic => $"{diagnostic.Code}:{diagnostic.Message}"));
    }

    private static string Describe(LorqPackageJudgementResult result)
    {
        return string.Join("; ", result.Diagnostics.Select(diagnostic => $"{diagnostic.Code}:{diagnostic.Message}"));
    }

    private static string Describe(LorqPackageReportResult result)
    {
        return string.Join("; ", result.Diagnostics.Select(diagnostic => $"{diagnostic.Code}:{diagnostic.Message}"));
    }

    private static string Describe(PackageValidationResult result)
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
            return new TemporaryDirectory(Directory.CreateTempSubdirectory("lorq-dotnet-full-loop-").FullName);
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
