using Lorq.Core;
using Lorq.Reporting;

namespace Lorq.Cli.Commands;

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

        var result = await DeterministicRunShardApplication.RunAsync(options, cancellationToken);
        var summary = ValidationSummaryRenderer.FromRunShardWriteResult(result);
        return result.Ok ? CommandResult.Success(summary) : CommandResult.Failure(summary);
    }
}
