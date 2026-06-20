using Lorq.Core;
using Lorq.Reporting;
using Lorq.Cli.Commands;
using Lorq.Cli.Commands.Parsing;
using Lorq.Cli.Commands.Results;

namespace Lorq.Cli.Commands.Handlers;

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
