namespace Lorq.Reporting;

/// <summary>
/// Request for rendering a deterministic LORQ package report.
/// </summary>
public sealed record LorqPackageReportRequest(
    string PackageRoot,
    string PrimaryJudgement = "judge-primary");
