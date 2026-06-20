using Lorq.Reporting;
using Lorq.Cli.Commands;
using Lorq.Cli.Commands.Parsing;
using Lorq.Cli.Commands.Results;

namespace Lorq.Cli.Commands.Handlers;

public sealed class ReportPackageCommandHandler : ICommandHandler<ReportPackageOptions>
{
    public ValueTask<CommandResult> HandleAsync(ReportPackageOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        var request = new LorqPackageReportRequest(options.PackageRoot, options.PrimaryJudgement);
        var result = LorqPackageReportRenderer.Render(request);
        var payload = ValidationSummaryRenderer.FromPackageReportResult(result);
        return ValueTask.FromResult(result.Ok ? CommandResult.Success(payload) : CommandResult.Failure(payload));
    }
}
