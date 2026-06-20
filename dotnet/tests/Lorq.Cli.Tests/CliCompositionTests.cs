using Lorq.Cli;
using Lorq.Cli.Commands;
using Lorq.Cli.Hosting;
using Microsoft.Extensions.DependencyInjection;
using TUnit.Assertions;
using TUnit.Core;

namespace Lorq.Cli.Tests;

public sealed class CliCompositionTests
{
    [Test]
    public async Task HostRegistersCliApplicationAndCommandCatalog()
    {
        using var host = LorqCliHost.Build(Array.Empty<string>());

        var application = host.Services.GetRequiredService<LorqCliApplication>();
        var catalog = host.Services.GetRequiredService<LorqCommandCatalog>();

        await Assert.That(application).IsNotNull();
        await Assert.That(catalog.TryFind("validate-package", out _)).IsTrue();
        await Assert.That(catalog.TryFind("run", out _)).IsTrue();
    }

    [Test]
    public async Task HostBackedApplicationStillExecutesCommands()
    {
        var packageRoot = Path.Combine(TestPaths.RepoRoot(), "fixtures", "golden", "deterministic-orchestration", "experiment-001");
        using var host = LorqCliHost.Build(Array.Empty<string>());
        var application = host.Services.GetRequiredService<LorqCliApplication>();
        var output = new StringWriter();
        var error = new StringWriter();

        var exitCode = await application.ExecuteAsync(new[] { "validate-package", packageRoot }, output, error);

        await Assert.That(exitCode).IsEqualTo(0).Because(error.ToString());
        await Assert.That(output.ToString()).Contains("\"ok\": true");
        await Assert.That(error.ToString()).IsEmpty();
    }
}
