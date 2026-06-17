using Lorq.Core;
using TUnit.Assertions;
using TUnit.Core;

namespace Lorq.Core.Tests;

public sealed class PackageIndexRebuildTests
{
    private readonly string goldenExperiment = Path.Combine(
        TestPaths.RepoRoot(),
        "fixtures",
        "golden",
        "deterministic-orchestration",
        "experiment-001");

    [Test]
    public async Task RebuildsGoldenExperimentIndexesByteStable()
    {
        using var target = TemporaryDirectory.Create();

        var result = LorqPackageIndexRebuilder.Rebuild(goldenExperiment, target.Path);

        await Assert.That(result.Ok).IsTrue().Because(Describe(result));
        await Assert.That(result.GeneratedFiles.Count).IsEqualTo(14);
        foreach (var relativePath in result.GeneratedFiles)
        {
            await AssertStable(relativePath, target.Path);
        }
    }

    [Test]
    public async Task RebuiltIndexesCanValidateWhenOverlayedOnPackageCopy()
    {
        using var target = TemporaryDirectory.Create();
        CopyDirectory(goldenExperiment, target.Path);
        Directory.Delete(Path.Combine(target.Path, ".lorq"), recursive: true);

        var result = LorqPackageIndexRebuilder.Rebuild(goldenExperiment, target.Path);
        var validation = LorqPackageValidator.Validate(target.Path);

        await Assert.That(result.Ok).IsTrue().Because(Describe(result));
        await Assert.That(validation.Ok).IsTrue().Because(DescribePackage(validation));
    }

    private async Task AssertStable(string relativePath, string targetRoot)
    {
        var expectedPath = Path.Combine(goldenExperiment, ".lorq", relativePath.Replace('/', Path.DirectorySeparatorChar));
        var actualPath = Path.Combine(targetRoot, ".lorq", relativePath.Replace('/', Path.DirectorySeparatorChar));
        await Assert.That(File.Exists(actualPath)).IsTrue().Because(actualPath);
        await Assert.That(await File.ReadAllTextAsync(actualPath)).IsEqualTo(await File.ReadAllTextAsync(expectedPath)).Because(relativePath);
    }

    private static string Describe(LorqIndexRebuildResult result)
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
            return new TemporaryDirectory(Directory.CreateTempSubdirectory("lorq-index-rebuild-").FullName);
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
