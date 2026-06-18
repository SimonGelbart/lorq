using Lorq.Core;
using TUnit.Assertions;
using TUnit.Core;

namespace Lorq.Core.Tests;

public sealed class PackageJudgementTests
{
    private readonly string goldenRoot = Path.Combine(TestPaths.RepoRoot(), "fixtures", "golden", "deterministic-orchestration");
    private readonly string benchmarkPath = Path.Combine(TestPaths.RepoRoot(), "fixtures", "conformance", "deterministic-orchestration", "benchmark.yaml");
    private readonly string fixturePath = Path.Combine(TestPaths.RepoRoot(), "fixtures", "conformance", "deterministic-orchestration", "fixtures", "fake-judge.yaml");

    [Test]
    public async Task AttachesDeterministicJudgementToMergedPackage()
    {
        using var workspace = MergedPackageWorkspace();

        var result = LorqDeterministicPackageJudge.Attach(
            workspace.ExperimentRoot,
            "judge-primary",
            fixturePath);
        var validation = LorqPackageValidator.Validate(workspace.ExperimentRoot);

        await Assert.That(result.Ok).IsTrue().Because(Describe(result));
        await Assert.That(result.CellCount).IsEqualTo(8);
        await Assert.That(result.JudgedCellCount).IsEqualTo(8);
        await Assert.That(result.MissingExpectedCellIds).Contains("skipped-coverage__graphify-plus__attempt-001");
        await Assert.That(validation.Ok).IsTrue().Because(DescribePackage(validation));
        await Assert.That(validation.Package!.Judgements.Count).IsEqualTo(1);
        await Assert.That(validation.Package.Judgements.Single().RealLlmUsed).IsFalse();
    }

    [Test]
    public async Task JudgementOutputsMatchPythonBaselineBytes()
    {
        using var workspace = MergedPackageWorkspace();

        var result = LorqDeterministicPackageJudge.Attach(
            workspace.ExperimentRoot,
            "judge-primary",
            fixturePath);

        await Assert.That(result.Ok).IsTrue().Because(Describe(result));
        foreach (var relativePath in JudgementPaths())
        {
            await AssertFileBytes(relativePath, workspace.ExperimentRoot);
        }
    }

    [Test]
    public async Task MissingFixtureEntriesFailStrictly()
    {
        using var workspace = MergedPackageWorkspace();
        var incompleteFixture = Path.Combine(workspace.Root, "incomplete-fake-judge.yaml");
        await File.WriteAllTextAsync(incompleteFixture, IncompleteJudgeFixture());

        var result = LorqDeterministicPackageJudge.Attach(
            workspace.ExperimentRoot,
            "judge-primary",
            incompleteFixture);

        await Assert.That(result.Ok).IsFalse();
        await Assert.That(result.MissingFixtureCellIds.Count).IsEqualTo(7);
        await Assert.That(result.Errors.Any(diagnostic => diagnostic.Code == "LORQ310")).IsTrue().Because(Describe(result));
    }

    private MergedWorkspace MergedPackageWorkspace()
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

        return new MergedWorkspace(workspace, experimentRoot);
    }

    private static IReadOnlyList<string> JudgementPaths()
    {
        return new[]
        {
            ".lorq/judgements/judge-primary.json",
            "judgements/judge-primary/judgement.manifest.json",
            "judgements/judge-primary/judgement.summary.json",
            "judgements/judge-primary/cells/no-final-answer__baseline__attempt-001.json",
            "judgements/judge-primary/cells/no-final-answer__graphify-plus__attempt-001.json",
            "judgements/judge-primary/cells/no-final-answer__graphify__attempt-001.json",
            "judgements/judge-primary/cells/skipped-coverage__baseline__attempt-001.json",
            "judgements/judge-primary/cells/skipped-coverage__graphify__attempt-001.json",
            "judgements/judge-primary/cells/successful-comparison__baseline__attempt-001.json",
            "judgements/judge-primary/cells/successful-comparison__graphify-plus__attempt-001.json",
            "judgements/judge-primary/cells/successful-comparison__graphify__attempt-001.json",
        };
    }

    private async Task AssertFileBytes(string relativePath, string targetRoot)
    {
        var expected = Path.Combine(goldenRoot, "experiment-001", relativePath.Replace('/', Path.DirectorySeparatorChar));
        var actual = Path.Combine(targetRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        await Assert.That(File.Exists(actual)).IsTrue().Because(actual);
        await Assert.That(await File.ReadAllTextAsync(actual)).IsEqualTo(await File.ReadAllTextAsync(expected)).Because(relativePath);
    }

    private static string IncompleteJudgeFixture()
    {
        return """
schema_version: lorq.fake-judge-fixture.v1alpha1
judgements:
- case: successful-comparison
  mode: baseline
  attempt: 1
  ok: true
  overall_score: 3
  confidence: high
  dimensions:
    correctness:
      score: 3
      rationale: Fixture score is deterministic.
    completeness:
      score: 3
      rationale: Fixture coverage is predefined.
    evidence:
      score: 3
      rationale: Evidence is present only when final answer is present.
  strengths:
  - source-backed deterministic answer
  weaknesses: []
  missing_or_questionable: []
  summary: baseline fixture answer is deterministically scored for successful comparison.
  elapsed_ms: 5
""";
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
        public string Root => Directory.Path;

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
            return new TemporaryDirectory(Directory.CreateTempSubdirectory("lorq-package-judge-").FullName);
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
