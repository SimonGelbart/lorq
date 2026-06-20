using Lorq.Adapters.Process;
using Lorq.Core;
using Lorq.Reporting;
using Lorq.Cli.Commands;
using Lorq.Cli.Commands.Parsing;
using Lorq.Cli.Commands.Results;
using Lorq.Cli.Runtime;

namespace Lorq.Cli.Commands.Handlers;

public sealed class RunCommandHandler : ICommandHandler<RunOptions>
{
    public async ValueTask<CommandResult> HandleAsync(RunOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (!options.NoJudge)
        {
            var payload = new { ok = false, diagnostics = new[] { new LorqDiagnostic("LORQ500", "error", "Only run --no-judge is implemented in this migration slice.") } };
            return CommandResult.Failure(payload);
        }

        try
        {
            var result = await DeterministicRunShardApplication.RunAsync(options, cancellationToken);
            var summary = ValidationSummaryRenderer.FromRunShardWriteResult(result);
            return result.Ok ? CommandResult.Success(summary) : CommandResult.Failure(summary);
        }
        catch (FileAdapterProtocolException exception)
        {
            var payload = new { ok = false, diagnostics = new[] { new LorqDiagnostic(exception.Code, "error", exception.Message) } };
            return CommandResult.Failure(payload);
        }
    }
}
