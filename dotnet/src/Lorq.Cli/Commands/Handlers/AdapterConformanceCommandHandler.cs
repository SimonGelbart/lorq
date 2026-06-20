using Lorq.Adapters.Process;
using Lorq.Cli.Commands;
using Lorq.Cli.Commands.Parsing;
using Lorq.Cli.Commands.Results;

namespace Lorq.Cli.Commands.Handlers;

internal sealed class AdapterConformanceCommandHandler : ICommandHandler<AdapterConformanceOptions>
{
    private readonly FileAdapterConformanceRunner runner;

    public AdapterConformanceCommandHandler(FileAdapterConformanceRunner runner)
    {
        ArgumentNullException.ThrowIfNull(runner);
        this.runner = runner;
    }

    public async ValueTask<CommandResult> HandleAsync(AdapterConformanceOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        var command = new FileAdapterProcessCommand(
            options.AdapterCommand,
            options.AdapterArguments,
            options.AdapterWorkingDirectory,
            new Dictionary<string, string>());
        var report = await runner.RunAsync(command, options.OutputRoot, options.TimeoutMilliseconds, cancellationToken).ConfigureAwait(false);
        return report.Ok ? CommandResult.Success(report) : CommandResult.Failure(report);
    }
}
