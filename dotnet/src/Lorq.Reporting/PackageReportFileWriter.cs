using System.Text.Json.Nodes;

namespace Lorq.Reporting;

internal sealed class PackageReportFileWriter
{
    private readonly PackageReportMarkdownRenderer reportMarkdownRenderer = new();
    private readonly CaseReviewMarkdownRenderer caseReviewMarkdownRenderer = new();

    public void Write(string packageRoot, PackageReportDocument document, string primaryJudgement)
    {
        ArgumentNullException.ThrowIfNull(packageRoot);
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(primaryJudgement);

        var reportsRoot = Path.Combine(packageRoot, "reports");
        Directory.CreateDirectory(Path.Combine(reportsRoot, "cases"));
        PackageReportJson.Write(Path.Combine(reportsRoot, "report.json"), document.Report);
        File.WriteAllText(Path.Combine(reportsRoot, "report.md"), reportMarkdownRenderer.Render(document.Report));
        WriteCasePacks(reportsRoot, document.CasePacks);
        WriteReportReference(packageRoot, document.CasePacks, primaryJudgement);
    }

    private void WriteCasePacks(string reportsRoot, IReadOnlyList<JsonObject> casePacks)
    {
        foreach (var pack in casePacks)
        {
            var caseId = PackageReportJson.StringProperty(pack, "case_id");
            var caseRoot = Path.Combine(reportsRoot, "cases", caseId);
            Directory.CreateDirectory(caseRoot);
            PackageReportJson.Write(Path.Combine(caseRoot, "case-review.json"), pack);
            File.WriteAllText(Path.Combine(caseRoot, "case-review.md"), caseReviewMarkdownRenderer.Render(pack));
        }
    }

    private static void WriteReportReference(string packageRoot, IReadOnlyList<JsonObject> casePacks, string primaryJudgement)
    {
        Directory.CreateDirectory(Path.Combine(packageRoot, ".lorq"));
        PackageReportJson.Write(Path.Combine(packageRoot, ".lorq", "report.json"), new JsonObject
        {
            ["schema_version"] = "lorq.report.v1alpha1",
            ["contract_version"] = PackageReportContract.Version,
            ["report"] = "reports/report.json",
            ["markdown"] = "reports/report.md",
            ["primary_judgement"] = primaryJudgement,
            ["case_count"] = casePacks.Count,
        });
    }
}
