using Lorq.Cli;
using TUnit.Assertions;
using TUnit.Core;

namespace Lorq.Cli.Tests;

public sealed class CliApplicationTests
{
    private readonly string packageRoot = Path.Combine(TestPaths.RepoRoot(), "fixtures", "golden", "deterministic-orchestration", "experiment-001");

    [Test]
    public async Task ValidatePackageCommandWritesJsonSummary()
    {
        var output = new StringWriter();
        var error = new StringWriter();

        var exitCode = await LorqCliApplication.RunAsync(new[] { "validate-package", packageRoot }, output, error);

        await Assert.That(exitCode).IsEqualTo(0).Because(error.ToString());
        await Assert.That(output.ToString()).Contains("\"ok\": true");
        await Assert.That(output.ToString()).Contains("\"cell_count\": 8");
        await Assert.That(error.ToString()).IsEmpty();
    }

    [Test]
    public async Task AdapterConformanceCommandWritesJsonSummary()
    {
        using var outputRoot = TemporaryDirectory.Create();
        var output = new StringWriter();
        var error = new StringWriter();

        var exitCode = await LorqCliApplication.RunAsync(new[]
        {
            "adapter-conformance",
            "--adapter-command",
            DotnetExecutable(),
            "--adapter-arg",
            TestHostDll(),
            "--adapter-working-directory",
            TestPaths.RepoRoot(),
            "--out",
            outputRoot.Path,
        }, output, error);

        await Assert.That(exitCode).IsEqualTo(0).Because(error.ToString());
        await Assert.That(output.ToString()).Contains("\"ok\": true");
        await Assert.That(output.ToString()).Contains("basic-exchange");
        await Assert.That(error.ToString()).IsEmpty();
    }

    [Test]
    public async Task AdapterCommandGroupConformanceWritesJsonSummary()
    {
        using var outputRoot = TemporaryDirectory.Create();
        var output = new StringWriter();
        var error = new StringWriter();

        var exitCode = await LorqCliApplication.RunAsync(new[]
        {
            "adapter",
            "conformance",
            "--adapter-command",
            DotnetExecutable(),
            "--adapter-arg",
            TestHostDll(),
            "--adapter-working-directory",
            TestPaths.RepoRoot(),
            "--out",
            outputRoot.Path,
        }, output, error);

        await Assert.That(exitCode).IsEqualTo(0).Because(error.ToString());
        await Assert.That(output.ToString()).Contains("\"ok\": true");
        await Assert.That(output.ToString()).Contains("basic-exchange");
        await Assert.That(error.ToString()).IsEmpty();
    }

    [Test]
    public async Task AdapterConformanceCommandReturnsFailureForBadAdapter()
    {
        using var outputRoot = TemporaryDirectory.Create();
        var output = new StringWriter();
        var error = new StringWriter();

        var exitCode = await LorqCliApplication.RunAsync(new[]
        {
            "adapter-conformance",
            "--adapter-command",
            DotnetExecutable(),
            "--adapter-arg",
            TestHostDll(),
            "--adapter-arg",
            "--write-malformed-evidence",
            "--adapter-working-directory",
            TestPaths.RepoRoot(),
            "--out",
            outputRoot.Path,
        }, output, error);

        await Assert.That(exitCode).IsEqualTo(1);
        await Assert.That(output.ToString()).Contains("LORQ-ADAPTER-EVIDENCE-INVALID");
        await Assert.That(error.ToString()).IsEmpty();
    }

    [Test]
    public async Task UnknownCommandReturnsUsageExitCode()
    {
        var output = new StringWriter();
        var error = new StringWriter();

        var exitCode = await LorqCliApplication.RunAsync(new[] { "unknown", packageRoot }, output, error);

        await Assert.That(exitCode).IsEqualTo(2);
        await Assert.That(output.ToString()).IsEmpty();
        await Assert.That(error.ToString()).Contains("Unknown command");
    }

    private static string TestHostDll()
    {
        var root = Path.Combine(TestPaths.RepoRoot(), "dotnet", "tests", "Lorq.Adapter.TestHost", "bin");
        if (!Directory.Exists(root))
        {
            throw new DirectoryNotFoundException(root);
        }

        var candidates = Directory
            .EnumerateFiles(root, "Lorq.Adapter.TestHost.dll", SearchOption.AllDirectories)
            .Where(path => path.Contains($"{Path.DirectorySeparatorChar}net10.0{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .OrderByDescending(path => path.Contains($"{Path.DirectorySeparatorChar}Release{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .ThenBy(path => path, StringComparer.Ordinal)
            .ToArray();

        return candidates.Length > 0
            ? candidates[0]
            : throw new FileNotFoundException("Lorq.Adapter.TestHost.dll was not found under " + root);
    }

    private static string DotnetExecutable()
    {
        return Environment.GetEnvironmentVariable("DOTNET_ROOT") is { Length: > 0 } dotnetRoot
            ? Path.Combine(dotnetRoot, "dotnet")
            : "dotnet";
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
            return new TemporaryDirectory(Directory.CreateTempSubdirectory("lorq-cli-adapter-conformance-test-").FullName);
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
