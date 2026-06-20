using System.Text.Json.Nodes;

namespace Lorq.Reporting;

internal sealed class CaseReviewMarkdownRenderer
{
    public string Render(JsonObject casePack)
    {
        ArgumentNullException.ThrowIfNull(casePack);

        var lines = new List<string>
        {
            $"# LORQ case review: {PackageReportJson.StringProperty(casePack, "case_id")}",
            "",
            $"Cells: {PackageReportJson.OptionalInt(casePack, "cell_count", 0)}",
            $"Missing expected cells: {PackageReportJson.OptionalInt(casePack, "missing_expected_cell_count", 0)}",
            $"Average score: {PackageReportFormatting.DecimalText(casePack["score_summary"]?["average"]?.GetValue<decimal>())}",
            "",
            "| Cell | Mode | Status | Score | Final answer |",
            "| --- | --- | --- | ---: | --- |",
        };
        AddCells(lines, casePack);
        AddMissingExpectedCellIds(lines, casePack);
        return string.Join('\n', lines) + "\n";
    }

    private static void AddCells(List<string> lines, JsonObject casePack)
    {
        foreach (var cell in casePack["cells"]!.AsArray().OfType<JsonObject>())
        {
            lines.Add($"| `{PackageReportJson.StringProperty(cell, "cell_id")}` | `{PackageReportJson.StringProperty(cell, "mode_id")}` | {PackageReportJson.StringProperty(cell, "status")} | {PackageReportFormatting.DecimalText(cell["score"]?.GetValue<decimal>())} | {PackageReportFormatting.BoolText(cell["final_answer_present"]?.GetValue<bool>() ?? false)} |");
        }
    }

    private static void AddMissingExpectedCellIds(List<string> lines, JsonObject casePack)
    {
        var missingExpectedCellIds = PackageReportJson.StringArrayValues(casePack["missing_expected_cell_ids"]);
        if (missingExpectedCellIds.Count == 0)
        {
            return;
        }

        lines.AddRange(new[] { "", "## Missing expected cells" });
        foreach (var cellId in missingExpectedCellIds)
        {
            lines.Add($"- `{cellId}`");
        }
    }
}
