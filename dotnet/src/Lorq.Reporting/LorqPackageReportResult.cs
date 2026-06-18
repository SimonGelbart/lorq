using Lorq.Core;

namespace Lorq.Reporting;

/// <summary>
/// Result of rendering canonical package report artifacts.
/// </summary>
public sealed record LorqPackageReportResult(
    bool Ok,
    string PackageRoot,
    string PrimaryJudgement,
    string ReportJson,
    string ReportMarkdown,
    int CasePackCount,
    IReadOnlyList<string> MissingExpectedCellIds,
    IReadOnlyDictionary<string, object?> ScoreSummary,
    IReadOnlyList<LorqDiagnostic> Diagnostics)
{
    public IReadOnlyList<LorqDiagnostic> Errors => Diagnostics.Where(diagnostic => diagnostic.Severity == "error").ToArray();
}
