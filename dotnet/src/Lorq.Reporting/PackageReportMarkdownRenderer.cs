using System.Text.Json.Nodes;

namespace Lorq.Reporting;

internal sealed class PackageReportMarkdownRenderer
{
    public string Render(JsonObject report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var summary = report["summary"]!.AsObject();
        var package = report["package"]!.AsObject();
        var judgement = report["primary_judgement"]!.AsObject();
        var lines = new List<string>
        {
            "# LORQ package report",
            "",
            $"Package: `{PackageReportJson.StringProperty(package, "package_id")}`",
            $"Kind: `{PackageReportJson.StringProperty(package, "package_kind")}`",
            $"Primary judgement: `{PackageReportJson.StringProperty(judgement, "name")}`",
            $"Real LLM used for judgement: `{PackageReportFormatting.BoolText(judgement["source"]?["real_llm_used"]?.GetValue<bool>() ?? false)}`",
            "",
            "## Summary",
            "",
            $"- Cells: {PackageReportJson.OptionalInt(summary, "cell_count", 0)} present / {PackageReportJson.OptionalInt(summary, "expected_cell_count", 0)} expected",
            $"- Missing expected cells: {PackageReportJson.OptionalInt(summary, "missing_expected_cell_count", 0)}",
            $"- Integrity OK: {PackageReportFormatting.BoolText(summary["integrity_ok"]?.GetValue<bool>() ?? false)}",
            $"- Warning count: {PackageReportJson.OptionalInt(summary, "warning_count", 0)}",
            $"- Average score: {PackageReportFormatting.DecimalText(summary["score_summary"]?["overall_average"]?.GetValue<decimal>())}",
            "",
            "## Status counts",
            "",
        };
        AddStatusCounts(lines, summary);
        AddMissingExpectedCellIds(lines, summary);
        AddCaseTable(lines, report);
        return string.Join('\n', lines) + "\n";
    }

    private static void AddStatusCounts(List<string> lines, JsonObject summary)
    {
        foreach (var status in summary["status_counts"]!.AsObject())
        {
            lines.Add($"- `{status.Key}`: {status.Value?.GetValue<int>() ?? 0}");
        }
    }

    private static void AddMissingExpectedCellIds(List<string> lines, JsonObject summary)
    {
        var missingExpectedCellIds = PackageReportJson.StringArrayValues(summary["missing_expected_cell_ids"]);
        if (missingExpectedCellIds.Count == 0)
        {
            return;
        }

        lines.AddRange(new[] { "", "## Missing expected cells", "" });
        foreach (var cellId in missingExpectedCellIds)
        {
            lines.Add($"- `{cellId}`");
        }
    }

    private static void AddCaseTable(List<string> lines, JsonObject report)
    {
        lines.AddRange(new[]
        {
            "",
            "## Cases",
            "",
            "| Case | Cells | Missing | Average score |",
            "| --- | ---: | ---: | ---: |",
        });
        foreach (var casePack in report["case_packs"]!.AsArray().OfType<JsonObject>())
        {
            lines.Add($"| `{PackageReportJson.StringProperty(casePack, "case_id")}` | {PackageReportJson.OptionalInt(casePack, "cell_count", 0)} | {PackageReportJson.OptionalInt(casePack, "missing_expected_cell_count", 0)} | {PackageReportFormatting.DecimalText(casePack["score_summary"]?["average"]?.GetValue<decimal>())} |");
        }
    }
}
