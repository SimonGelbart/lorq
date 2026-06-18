using Lorq.Core;
using Lorq.Reporting;

namespace Lorq.Cli.Commands;

public sealed class JudgePackageCommandHandler : ICommandHandler<JudgePackageOptions>
{
    public ValueTask<CommandResult> HandleAsync(JudgePackageOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        var request = new LorqPackageJudgeRequest(options.PackageRoot, options.JudgeName, options.FixturePath, options.Strict);
        var result = LorqDeterministicPackageJudge.Attach(request);
        var payload = ValidationSummaryRenderer.FromPackageJudgementResult(result);
        return ValueTask.FromResult(result.Ok ? CommandResult.Success(payload) : CommandResult.Failure(payload));
    }
}
