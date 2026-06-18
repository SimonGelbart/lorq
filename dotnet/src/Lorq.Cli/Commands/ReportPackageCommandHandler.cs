using Lorq.Reporting;

namespace Lorq.Cli.Commands;

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
