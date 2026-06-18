using Lorq.Core;
using Lorq.Reporting;
using TUnit.Assertions;
using TUnit.Core;

namespace Lorq.Core.Tests;

public sealed class PackageReportTests
{
    private readonly string goldenRoot = Path.Combine(TestPaths.RepoRoot(), "fixtures", "golden", "deterministic-orchestration");
    private readonly string benchmarkPath = Path.Combine(TestPaths.RepoRoot(), "fixtures", "conformance", "deterministic-orchestration", "benchmark.yaml");
    private readonly string fixturePath = Path.Combine(TestPaths.RepoRoot(), "fixtures", "conformance", "deterministic-orchestration", "fixtures", "fake-judge.yaml");

    [Test]
    public async Task RendersDeterministicPackageReport()
    {
        using var workspace = JudgedPackageWorkspace();

        var result = LorqPackageReportRenderer.Render(workspace.ExperimentRoot, "judge-primary");
        var validation = LorqPackageValidator.Validate(workspace.ExperimentRoot);

        await Assert.That(result.Ok).IsTrue().Because(Describe(result));
        await Assert.That(result.CasePackCount).IsEqualTo(3);
        await Assert.That(result.MissingExpectedCellIds).Contains("skipped-coverage__graphify-plus__attempt-001");
        await Assert.That(validation.Ok).IsTrue().Because(DescribePackage(validation));
        await Assert.That(validation.Package!.Report).IsNotNull();
    }

    [Test]
    public async Task ReportOutputsMatchPythonBaselineBytes()
    {
        using var workspace = JudgedPackageWorkspace();

        var result = LorqPackageReportRenderer.Render(workspace.ExperimentRoot, "judge-primary");

        await Assert.That(result.Ok).IsTrue().Because(Describe(result));
        foreach (var relativePath in ReportPaths())
        {
            await AssertFileBytes(relativePath, workspace.ExperimentRoot);
        }
    }

    private MergedWorkspace JudgedPackageWorkspace()
    {
        var workspace = TemporaryDirectory.Create();
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
        if (!merge.Ok)
        {
            throw new InvalidOperationException(Describe(merge));
        }

        var judgement = LorqDeterministicPackageJudge.Attach(experimentRoot, "judge-primary", fixturePath);
        if (!judgement.Ok)
        {
            throw new InvalidOperationException(Describe(judgement));
        }

        return new MergedWorkspace(workspace, experimentRoot);
    }

    private static IReadOnlyList<string> ReportPaths()
    {
        return new[]
        {
            ".lorq/report.json",
            "reports/report.json",
            "reports/report.md",
            "reports/cases/no-final-answer/case-review.json",
            "reports/cases/no-final-answer/case-review.md",
            "reports/cases/skipped-coverage/case-review.json",
            "reports/cases/skipped-coverage/case-review.md",
            "reports/cases/successful-comparison/case-review.json",
            "reports/cases/successful-comparison/case-review.md",
        };
    }

    private async Task AssertFileBytes(string relativePath, string targetRoot)
    {
        var expected = Path.Combine(goldenRoot, "experiment-001", relativePath.Replace('/', Path.DirectorySeparatorChar));
        var actual = Path.Combine(targetRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        await Assert.That(File.Exists(actual)).IsTrue().Because(actual);
        await Assert.That(await File.ReadAllTextAsync(actual)).IsEqualTo(await File.ReadAllTextAsync(expected)).Because(relativePath);
    }

    private static string Describe(LorqPackageReportResult result)
    {
        return string.Join("; ", result.Diagnostics.Select(diagnostic => $"{diagnostic.Code}:{diagnostic.Message}"));
    }

    private static string Describe(LorqPackageJudgementResult result)
    {
        return string.Join("; ", result.Diagnostics.Select(diagnostic => $"{diagnostic.Code}:{diagnostic.Message}"));
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

    private sealed record MergedWorkspace(TemporaryDirectory Directory, string ExperimentRoot) : IDisposable
    {
        public void Dispose()
        {
            Directory.Dispose();
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
            return new TemporaryDirectory(Directory.CreateTempSubdirectory("lorq-package-report-").FullName);
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
