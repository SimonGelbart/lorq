using Lorq.Core;
using Lorq.Reporting;

namespace Lorq.Cli.Commands;

public sealed class ValidateMergeInputsCommandHandler : ICommandHandler<ValidateMergeInputsOptions>
{
    public ValueTask<CommandResult> HandleAsync(ValidateMergeInputsOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        var result = LorqPackageValidator.ValidateMergeInputs(options.ShardRoots);
        var payload = ValidationSummaryRenderer.FromMergeInputResult(result);
        return ValueTask.FromResult(result.Ok ? CommandResult.Success(payload) : CommandResult.Failure(payload));
    }
}
