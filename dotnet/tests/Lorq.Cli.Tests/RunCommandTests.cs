using Lorq.Cli;
using TUnit.Assertions;
using TUnit.Core;

namespace Lorq.Cli.Tests;

public sealed class RunCommandTests
{
    private readonly string suiteRoot = Path.Combine(TestPaths.RepoRoot(), "fixtures", "conformance", "deterministic-orchestration");

    [Test]
    public async Task RunNoJudgeWritesValidDeterministicShard()
    {
        using var workspace = TemporaryDirectory.Create();
        var shardRoot = Path.Combine(workspace.Path, "shard-001");
        var output = new StringWriter();
        var error = new StringWriter();

        var exitCode = await LorqCliApplication.RunAsync(new[] { "run", "--no-judge", "--suite-root", suiteRoot, "--out", shardRoot }, output, error);
        var validationCode = await LorqCliApplication.RunAsync(new[] { "validate-package", shardRoot }, new StringWriter(), error);

        await Assert.That(exitCode).IsEqualTo(0).Because(error.ToString());
        await Assert.That(validationCode).IsEqualTo(0).Because(error.ToString());
        await Assert.That(File.Exists(Path.Combine(shardRoot, "runs", "shard-001", "shard.manifest.json"))).IsTrue();
        await Assert.That(output.ToString()).Contains("\"cell_count\": 3");
        await Assert.That(error.ToString()).IsEmpty();
    }


    [Test]
    public async Task RunNoJudgeCanUseExternalProcessAdapter()
    {
        using var workspace = TemporaryDirectory.Create();
        var shardRoot = Path.Combine(workspace.Path, "shard-001");
        var output = new StringWriter();
        var error = new StringWriter();

        var exitCode = await LorqCliApplication.RunAsync(new[]
        {
            "run",
            "--no-judge",
            "--suite-root",
            suiteRoot,
            "--out",
            shardRoot,
            "--adapter-command",
            DotnetExecutable(),
            "--adapter-arg",
            TestHostDll(),
        }, output, error);

        var evidencePath = Path.Combine(shardRoot, "runs", "shard-001", "cells", "successful-comparison__baseline__attempt-001", "adapter.evidence.json");
        await Assert.That(exitCode).IsEqualTo(0).Because(error.ToString());
        await Assert.That(File.ReadAllText(evidencePath)).Contains("external-test-adapter");
        await Assert.That(output.ToString()).Contains("\"cell_count\": 3");
        await Assert.That(error.ToString()).IsEmpty();
    }

    [Test]
    public async Task RunWithoutNoJudgeFailsDuringParsing()
    {
        using var workspace = TemporaryDirectory.Create();
        var output = new StringWriter();
        var error = new StringWriter();

        var exitCode = await LorqCliApplication.RunAsync(new[] { "run", "--suite-root", suiteRoot, "--out", Path.Combine(workspace.Path, "shard-001") }, output, error);

        await Assert.That(exitCode).IsEqualTo(2);
        await Assert.That(output.ToString()).IsEmpty();
        await Assert.That(error.ToString()).Contains("Only run --no-judge");
    }


    private static string TestHostDll()
    {
        return Path.Combine(TestPaths.RepoRoot(), "dotnet", "tests", "Lorq.Adapter.TestHost", "bin", "Debug", "net10.0", "Lorq.Adapter.TestHost.dll");
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
            return new TemporaryDirectory(Directory.CreateTempSubdirectory("lorq-cli-run-").FullName);
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
