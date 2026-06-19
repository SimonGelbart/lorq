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
        var cellId = "successful-comparison__baseline__attempt-001";
        var workspaceRoot = Path.Combine(shardRoot + ".workspaces", cellId);

        await Assert.That(File.Exists(Path.Combine(shardRoot, "runs", "shard-001", "shard.manifest.json"))).IsTrue();
        await Assert.That(File.Exists(Path.Combine(workspaceRoot, "src", "ledger.py"))).IsTrue();
        await Assert.That(workspaceRoot).Contains(Path.Combine("shard-001.workspaces", cellId));
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
    public async Task RunNoJudgeCanUseCodexProfileWrapperWithoutRealCodex()
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
            "--adapter-arg",
            "--assert-codex-profile",
            "--adapter-profile",
            "codex-cli",
            "--codex-command",
            "codex-test",
            "--codex-arg",
            "exec",
            "--codex-arg",
            "--json",
        }, output, error);

        var evidencePath = Path.Combine(shardRoot, "runs", "shard-001", "cells", "successful-comparison__baseline__attempt-001", "adapter.evidence.json");
        await Assert.That(exitCode).IsEqualTo(0).Because(error.ToString());
        await Assert.That(File.ReadAllText(evidencePath)).Contains("codex-profile-test-adapter");
        await Assert.That(output.ToString()).Contains("\"cell_count\": 3");
    }



    [Test]
    public async Task RunNoJudgeCanMaterializeWorkspaceUnderCustomWorkRoot()
    {
        using var workspace = TemporaryDirectory.Create();
        var shardRoot = Path.Combine(workspace.Path, "shard-001");
        var workRoot = Path.Combine(workspace.Path, "work-root");
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
            "--work-root",
            workRoot,
        }, output, error);

        var cellId = "successful-comparison__baseline__attempt-001";
        var workspaceRoot = Path.Combine(workRoot, "shard-001", cellId);

        await Assert.That(exitCode).IsEqualTo(0).Because(error.ToString());
        await Assert.That(File.Exists(Path.Combine(workspaceRoot, "src", "ledger.py"))).IsTrue();
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
