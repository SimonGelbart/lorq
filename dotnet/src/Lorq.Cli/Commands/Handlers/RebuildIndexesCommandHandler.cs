using Lorq.Core;
using Lorq.Reporting;
using Lorq.Cli.Commands;
using Lorq.Cli.Commands.Parsing;
using Lorq.Cli.Commands.Results;

namespace Lorq.Cli.Commands.Handlers;

public sealed class RebuildIndexesCommandHandler : ICommandHandler<RebuildIndexesOptions>
{
    public ValueTask<CommandResult> HandleAsync(RebuildIndexesOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        var result = LorqPackageIndexRebuilder.Rebuild(options.PackageRoot, options.TargetRoot);
        var payload = ValidationSummaryRenderer.FromIndexRebuildResult(result);
        return ValueTask.FromResult(result.Ok ? CommandResult.Success(payload) : CommandResult.Failure(payload));
    }
}
