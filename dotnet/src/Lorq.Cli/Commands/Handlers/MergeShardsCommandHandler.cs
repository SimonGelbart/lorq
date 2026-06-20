using Lorq.Core;
using Lorq.Reporting;
using Lorq.Cli.Commands;
using Lorq.Cli.Commands.Parsing;
using Lorq.Cli.Commands.Results;

namespace Lorq.Cli.Commands.Handlers;

public sealed class MergeShardsCommandHandler : ICommandHandler<MergeShardsOptions>
{
    public ValueTask<CommandResult> HandleAsync(MergeShardsOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        var request = new LorqPackageMergeRequest(options.ShardRoots, options.OutputRoot, options.PackageId, options.BenchmarkPath, options.Strict);
        var result = LorqPackageMerger.Merge(request);
        var payload = ValidationSummaryRenderer.FromPackageMergeResult(result);
        return ValueTask.FromResult(result.Ok ? CommandResult.Success(payload) : CommandResult.Failure(payload));
    }
}
