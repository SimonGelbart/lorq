using System.Text.Json;
using System.Text.Json.Nodes;
using Lorq.Core;

namespace Lorq.Reporting;

internal sealed class PackageReportResultFactory
{
    public LorqPackageReportResult SuccessfulResult(
        LorqPackageReportRequest request,
        string packageRoot,
        PackageReportDocument document,
        IReadOnlyList<LorqDiagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(packageRoot);
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(diagnostics);

        return new LorqPackageReportResult(
            !diagnostics.Any(diagnostic => diagnostic.Severity == "error"),
            packageRoot,
            request.PrimaryJudgement,
            "reports/report.json",
            "reports/report.md",
            document.CasePacks.Count,
            document.MissingExpectedCellIds,
            ScoreSummaryAsDictionary(document.Report["summary"]?["score_summary"] as JsonObject),
            diagnostics.ToArray());
    }

    public LorqPackageReportResult FailedResult(
        LorqPackageReportRequest request,
        string packageRoot,
        IReadOnlyList<LorqDiagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(packageRoot);
        ArgumentNullException.ThrowIfNull(diagnostics);

        return new LorqPackageReportResult(
            false,
            packageRoot,
            request.PrimaryJudgement,
            "reports/report.json",
            "reports/report.md",
            0,
            Array.Empty<string>(),
            new Dictionary<string, object?>(),
            diagnostics.ToArray());
    }

    private static IReadOnlyDictionary<string, object?> ScoreSummaryAsDictionary(JsonObject? scoreSummary)
    {
        return JsonSerializer.Deserialize<Dictionary<string, object?>>(scoreSummary?.ToJsonString() ?? "{}") ?? new Dictionary<string, object?>();
    }
}
