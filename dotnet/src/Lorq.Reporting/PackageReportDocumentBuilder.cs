using System.Text.Json.Nodes;

namespace Lorq.Reporting;

internal sealed class PackageReportDocumentBuilder
{
    private const string ContractVersion = PackageReportContract.Version;

    private readonly PackageReportCasePackBuilder casePackBuilder = new();

    public PackageReportDocument Build(PackageReportInputs inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);

        var missingExpectedCellIds = PackageReportCoverage.MissingExpectedCellIds(inputs.Coverage);
        var casePacks = casePackBuilder.Build(inputs.Cells, inputs.Judgements, missingExpectedCellIds);
        var report = BuildReport(inputs, casePacks, missingExpectedCellIds);
        return new PackageReportDocument(report, casePacks, missingExpectedCellIds);
    }

    private static JsonObject BuildReport(
        PackageReportInputs inputs,
        IReadOnlyList<JsonObject> casePacks,
        IReadOnlyList<string> missingExpectedCellIds)
    {
        return new JsonObject
        {
            ["schema_version"] = "lorq.report.v1alpha1",
            ["contract_version"] = ContractVersion,
            ["package"] = new JsonObject
            {
                ["package_id"] = inputs.Package.PackageId,
                ["package_kind"] = inputs.Package.PackageKind,
                ["schema_version"] = inputs.Package.PackageSchemaVersion,
                ["shards"] = PackageReportJson.StringArray(inputs.Package.DeclaredShardIds),
                ["package_root"] = ".",
            },
            ["summary"] = new JsonObject
            {
                ["cell_count"] = inputs.Package.Cells.Count,
                ["expected_cell_count"] = PackageReportJson.OptionalInt(inputs.Coverage, "expected_cell_count", inputs.Package.Cells.Count),
                ["missing_expected_cell_count"] = missingExpectedCellIds.Count,
                ["missing_expected_cell_ids"] = PackageReportJson.StringArray(missingExpectedCellIds),
                ["status_counts"] = PackageReportJson.CloneObject(inputs.Coverage["status_counts"]),
                ["integrity_ok"] = inputs.Integrity["ok"]?.GetValue<bool>() ?? false,
                ["warning_count"] = PackageReportCoverage.WarningCount(inputs.Integrity),
                ["score_summary"] = PackageReportJson.CloneObject(inputs.JudgementManifest["score_summary"]),
            },
            ["primary_judgement"] = new JsonObject
            {
                ["name"] = inputs.PrimaryJudgement,
                ["backend"] = PackageReportJson.StringProperty(inputs.JudgementManifest, "backend"),
                ["source"] = PackageReportJson.CloneObject(inputs.JudgementManifest["source"]),
                ["cell_count"] = PackageReportJson.OptionalInt(inputs.JudgementManifest, "cell_count", 0),
                ["judged_cell_count"] = PackageReportJson.OptionalInt(inputs.JudgementManifest, "judged_cell_count", 0),
                ["scores_by_cell"] = ScoresByCell(inputs.Judgements),
            },
            ["integrity"] = PackageReportJson.CloneObject(inputs.Integrity),
            ["coverage"] = PackageReportJson.CloneObject(inputs.Coverage),
            ["fingerprints"] = new JsonObject
            {
                ["unique_fingerprint_count"] = PackageReportJson.OptionalInt(inputs.Fingerprints, "unique_fingerprint_count", 0),
            },
            ["case_packs"] = CasePackReferences(casePacks),
        };
    }

    private static JsonArray CasePackReferences(IReadOnlyList<JsonObject> casePacks)
    {
        var references = new JsonArray();
        foreach (var pack in casePacks)
        {
            var caseId = PackageReportJson.StringProperty(pack, "case_id");
            references.Add(new JsonObject
            {
                ["case_id"] = caseId,
                ["path"] = $"reports/cases/{caseId}/case-review.json",
                ["markdown_path"] = $"reports/cases/{caseId}/case-review.md",
                ["cell_count"] = PackageReportJson.OptionalInt(pack, "cell_count", 0),
                ["missing_expected_cell_count"] = PackageReportJson.OptionalInt(pack, "missing_expected_cell_count", 0),
                ["score_summary"] = PackageReportJson.CloneObject(pack["score_summary"]),
            });
        }

        return references;
    }

    private static JsonObject ScoresByCell(IReadOnlyDictionary<string, JsonObject> judgements)
    {
        var scores = new JsonObject();
        foreach (var item in judgements.OrderBy(item => item.Key, StringComparer.Ordinal))
        {
            scores[item.Key] = PackageReportFormatting.NullableDecimal(PackageReportJudgement.Score(item.Value));
        }

        return scores;
    }
}
