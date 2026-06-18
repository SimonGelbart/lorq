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
    public async Task UnknownCommandReturnsUsageExitCode()
    {
        var output = new StringWriter();
        var error = new StringWriter();

        var exitCode = await LorqCliApplication.RunAsync(new[] { "unknown", packageRoot }, output, error);

        await Assert.That(exitCode).IsEqualTo(2);
        await Assert.That(output.ToString()).IsEmpty();
        await Assert.That(error.ToString()).Contains("Unknown command");
    }
}
