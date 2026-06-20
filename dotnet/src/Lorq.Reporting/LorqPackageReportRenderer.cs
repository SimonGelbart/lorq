using Lorq.Core;

namespace Lorq.Reporting;

/// <summary>
/// Renders canonical deterministic LORQ package reports from judged experiment packages.
/// </summary>
public static class LorqPackageReportRenderer
{
    public static LorqPackageReportResult Render(string packageRoot, string primaryJudgement = "judge-primary")
    {
        return Render(new LorqPackageReportRequest(packageRoot, primaryJudgement));
    }

    public static LorqPackageReportResult Render(LorqPackageReportRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new PackageReportRenderingPipeline().Render(request);
    }
}
