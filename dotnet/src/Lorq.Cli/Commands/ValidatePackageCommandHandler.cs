using Lorq.Core;
using Lorq.Reporting;

namespace Lorq.Cli.Commands;

public sealed class ValidatePackageCommandHandler : ICommandHandler<ValidatePackageOptions>
{
    public ValueTask<CommandResult> HandleAsync(ValidatePackageOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        var result = LorqPackageValidator.Validate(options.PackageRoot);
        var payload = ValidationSummaryRenderer.FromPackageResult(result);
        return ValueTask.FromResult(result.Ok ? CommandResult.Success(payload) : CommandResult.Failure(payload));
    }
}
