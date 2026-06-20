using System.Text.Json.Nodes;

namespace Lorq.Reporting;

internal sealed class PackageReportCasePackBuilder
{
    private const string ContractVersion = PackageReportContract.Version;

    public IReadOnlyList<JsonObject> Build(
        IReadOnlyList<JsonObject> cells,
        IReadOnlyDictionary<string, JsonObject> judgements,
        IReadOnlyList<string> missingExpectedCellIds)
    {
        ArgumentNullException.ThrowIfNull(cells);
        ArgumentNullException.ThrowIfNull(judgements);
        ArgumentNullException.ThrowIfNull(missingExpectedCellIds);

        var packs = new List<JsonObject>();
        foreach (var caseId in CaseIds(cells, missingExpectedCellIds))
        {
            packs.Add(BuildCasePack(caseId, cells, judgements, missingExpectedCellIds));
        }

        return packs;
    }

    private static IReadOnlyList<string> CaseIds(IReadOnlyList<JsonObject> cells, IReadOnlyList<string> missingExpectedCellIds)
    {
        return cells.Select(cell => PackageReportJson.StringProperty(cell, "case_id"))
            .Concat(missingExpectedCellIds.Select(PackageReportCaseId.FromCellId))
            .Where(caseId => !string.IsNullOrWhiteSpace(caseId))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    private static JsonObject BuildCasePack(
        string caseId,
        IReadOnlyList<JsonObject> cells,
        IReadOnlyDictionary<string, JsonObject> judgements,
        IReadOnlyList<string> missingExpectedCellIds)
    {
        var caseCells = cells
            .Where(cell => PackageReportJson.StringProperty(cell, "case_id") == caseId)
            .OrderBy(cell => PackageReportJson.StringProperty(cell, "cell_id"), StringComparer.Ordinal)
            .ToArray();
        var caseMissing = missingExpectedCellIds
            .Where(cellId => cellId.StartsWith(caseId + "__", StringComparison.Ordinal))
            .ToArray();
        var rowCells = new JsonArray();
        var scores = new List<decimal>();
        foreach (var cell in caseCells)
        {
            rowCells.Add(BuildCaseCell(cell, judgements, scores));
        }

        return new JsonObject
        {
            ["schema_version"] = "lorq.case-review-pack.v1alpha1",
            ["contract_version"] = ContractVersion,
            ["case_id"] = caseId,
            ["cell_count"] = caseCells.Length,
            ["missing_expected_cell_count"] = caseMissing.Length,
            ["missing_expected_cell_ids"] = PackageReportJson.StringArray(caseMissing),
            ["score_summary"] = new JsonObject
            {
                ["average"] = PackageReportFormatting.NullableDecimal(PackageReportFormatting.Average(scores)),
                ["min"] = PackageReportFormatting.NullableDecimal(scores.Count == 0 ? null : scores.Min()),
                ["max"] = PackageReportFormatting.NullableDecimal(scores.Count == 0 ? null : scores.Max()),
            },
            ["cells"] = rowCells,
        };
    }

    private static JsonObject BuildCaseCell(JsonObject cell, IReadOnlyDictionary<string, JsonObject> judgements, List<decimal> scores)
    {
        var cellId = PackageReportJson.StringProperty(cell, "cell_id");
        judgements.TryGetValue(cellId, out var judgement);
        var score = PackageReportJudgement.Score(judgement);
        if (score is not null)
        {
            scores.Add(score.Value);
        }

        return new JsonObject
        {
            ["cell_id"] = cellId,
            ["mode_id"] = PackageReportJson.StringProperty(cell, "mode_id"),
            ["attempt_id"] = PackageReportJson.StringProperty(cell, "attempt_id"),
            ["status"] = PackageReportJson.StringProperty(cell, "status"),
            ["score"] = PackageReportFormatting.NullableDecimal(score),
            ["final_answer_present"] = cell["adapter_output"]?["final_answer_present"]?.GetValue<bool>() ?? false,
            ["evidence_refs"] = PackageReportJson.CloneObject(cell["evidence_refs"]),
            ["judgement_ref"] = judgement is null ? null : $"judgements/{PackageReportJson.StringProperty(judgement, "judgement_name")}/cells/{cellId}.json",
            ["integrity_warnings"] = new JsonArray(),
        };
    }
}
