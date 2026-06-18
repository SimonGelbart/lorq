using Lorq.Core;
using Lorq.Reporting;

namespace Lorq.Cli.Commands;

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
