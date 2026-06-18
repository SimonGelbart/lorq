using Lorq.Cli.Commands;
using TUnit.Assertions;
using TUnit.Core;

namespace Lorq.Cli.Tests;

public sealed class CommandOptionsParserTests
{
    [Test]
    public async Task ParsesMergeShardOptionsIntoACommandObject()
    {
        var result = LorqCommandOptionsParser.ParseMergeShards(new[]
        {
            "shard-001",
            "shard-002",
            "--out",
            "experiment-001",
            "--package-id",
            "deterministic-benchmark",
            "--benchmark",
            "benchmark.yaml",
            "--allow-incompatible",
        });

        await Assert.That(result.Ok).IsTrue().Because(result.ErrorMessage ?? "parse failed");
        await Assert.That(result.Options!.ShardRoots).IsEquivalentTo(new[] { "shard-001", "shard-002" });
        await Assert.That(result.Options.OutputRoot).IsEqualTo("experiment-001");
        await Assert.That(result.Options.PackageId).IsEqualTo("deterministic-benchmark");
        await Assert.That(result.Options.BenchmarkPath).IsEqualTo("benchmark.yaml");
        await Assert.That(result.Options.Strict).IsFalse();
    }


    [Test]
    public async Task ParsesExternalAdapterRunOptions()
    {
        var result = LorqCommandOptionsParser.ParseRun(new[]
        {
            "--no-judge",
            "--out",
            "shard-001",
            "--adapter-command",
            "dotnet",
            "--adapter-arg",
            "adapter.dll",
            "--adapter-working-directory",
            ".",
        });

        await Assert.That(result.Ok).IsTrue().Because(result.ErrorMessage ?? "parse failed");
        await Assert.That(result.Options!.AdapterCommand).IsEqualTo("dotnet");
        await Assert.That(result.Options.AdapterArguments).IsEquivalentTo(new[] { "adapter.dll" });
        await Assert.That(result.Options.AdapterWorkingDirectory).IsEqualTo(".");
    }

    [Test]
    public async Task RejectsIncompleteJudgeOptions()
    {
        var result = LorqCommandOptionsParser.ParseJudgePackage(new[] { "experiment-001", "--name", "judge-primary" });

        await Assert.That(result.Ok).IsFalse();
        await Assert.That(result.ErrorMessage).Contains("--fixture");
    }

    [Test]
    public async Task DefaultsReportPrimaryJudgement()
    {
        var result = LorqCommandOptionsParser.ParseReportPackage(new[] { "experiment-001" });

        await Assert.That(result.Ok).IsTrue().Because(result.ErrorMessage ?? "parse failed");
        await Assert.That(result.Options!.PrimaryJudgement).IsEqualTo("judge-primary");
    }
}
