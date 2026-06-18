using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Lorq.Core;

namespace Lorq.Reporting;

/// <summary>
/// Renders canonical deterministic LORQ package reports from judged experiment packages.
/// </summary>
public static class LorqPackageReportRenderer
{
    private const string ContractVersion = "lorq.contract.v1alpha1";

    private static readonly JsonSerializerOptions JsonWriterOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static LorqPackageReportResult Render(string packageRoot, string primaryJudgement = "judge-primary")
    {
        return Render(new LorqPackageReportRequest(packageRoot, primaryJudgement));
    }

    public static LorqPackageReportResult Render(LorqPackageReportRequest request)
    {
        var packageRoot = Path.GetFullPath(request.PackageRoot);
        var diagnostics = new List<LorqDiagnostic>();

        try
        {
            var validation = LorqPackageValidator.Validate(packageRoot);
            diagnostics.AddRange(validation.Diagnostics);
            if (validation.Package is null)
            {
                return FailedResult(request, packageRoot, diagnostics);
            }

            var cells = LoadPackageCells(packageRoot);
            if (cells.Count == 0)
            {
                diagnostics.Add(new LorqDiagnostic("LORQ410", "error", "Package has no LORQ cells to report.", packageRoot));
                return FailedResult(request, packageRoot, diagnostics);
            }

            var judgementManifest = LoadPrimaryJudgementManifest(packageRoot, request.PrimaryJudgement);
            var judgements = LoadPrimaryJudgements(packageRoot, request.PrimaryJudgement);
            var coverage = ReadJsonObject(Path.Combine(packageRoot, ".lorq", "coverage.json"));
            var integrity = ReadJsonObject(Path.Combine(packageRoot, ".lorq", "integrity.json"));
            var fingerprints = ReadJsonObject(Path.Combine(packageRoot, ".lorq", "fingerprints.json"));
            var missingExpectedCellIds = MissingExpectedCellIds(coverage);
            var casePacks = BuildCasePacks(cells, judgements, missingExpectedCellIds);
            var report = BuildReport(validation.Package, coverage, integrity, fingerprints, judgementManifest, judgements, casePacks, request.PrimaryJudgement);

            WriteReportFiles(packageRoot, report, casePacks, request.PrimaryJudgement);
            return SuccessfulResult(request, packageRoot, report, casePacks, missingExpectedCellIds, diagnostics);
        }
        catch (LorqPackageFormatException exception)
        {
            diagnostics.Add(new LorqDiagnostic("LORQ900", "error", exception.Message, packageRoot));
            return FailedResult(request, packageRoot, diagnostics);
        }
        catch (JsonException exception)
        {
            diagnostics.Add(new LorqDiagnostic("LORQ901", "error", $"Invalid JSON: {exception.Message}", packageRoot));
            return FailedResult(request, packageRoot, diagnostics);
        }
    }

    private static JsonObject BuildReport(
        ExperimentPackage package,
        JsonObject coverage,
        JsonObject integrity,
        JsonObject fingerprints,
        JsonObject judgementManifest,
        IReadOnlyDictionary<string, JsonObject> judgements,
        IReadOnlyList<JsonObject> casePacks,
        string primaryJudgement)
    {
        return new JsonObject
        {
            ["schema_version"] = "lorq.report.v1alpha1",
            ["contract_version"] = ContractVersion,
            ["package"] = new JsonObject
            {
                ["package_id"] = package.PackageId,
                ["package_kind"] = package.PackageKind,
                ["schema_version"] = package.PackageSchemaVersion,
                ["shards"] = StringArray(package.DeclaredShardIds),
                ["package_root"] = ".",
            },
            ["summary"] = new JsonObject
            {
                ["cell_count"] = package.Cells.Count,
                ["expected_cell_count"] = OptionalInt(coverage, "expected_cell_count", package.Cells.Count),
                ["missing_expected_cell_count"] = MissingExpectedCellIds(coverage).Count,
                ["missing_expected_cell_ids"] = StringArray(MissingExpectedCellIds(coverage)),
                ["status_counts"] = CloneObject(coverage["status_counts"]),
                ["integrity_ok"] = integrity["ok"]?.GetValue<bool>() ?? false,
                ["warning_count"] = WarningCount(integrity),
                ["score_summary"] = CloneObject(judgementManifest["score_summary"]),
            },
            ["primary_judgement"] = new JsonObject
            {
                ["name"] = primaryJudgement,
                ["backend"] = StringProperty(judgementManifest, "backend"),
                ["source"] = CloneObject(judgementManifest["source"]),
                ["cell_count"] = OptionalInt(judgementManifest, "cell_count", 0),
                ["judged_cell_count"] = OptionalInt(judgementManifest, "judged_cell_count", 0),
                ["scores_by_cell"] = ScoresByCell(judgements),
            },
            ["integrity"] = CloneObject(integrity),
            ["coverage"] = CloneObject(coverage),
            ["fingerprints"] = new JsonObject
            {
                ["unique_fingerprint_count"] = OptionalInt(fingerprints, "unique_fingerprint_count", 0),
            },
            ["case_packs"] = CasePackReferences(casePacks),
        };
    }

    private static IReadOnlyList<JsonObject> BuildCasePacks(
        IReadOnlyList<JsonObject> cells,
        IReadOnlyDictionary<string, JsonObject> judgements,
        IReadOnlyList<string> missingExpectedCellIds)
    {
        var caseIds = cells.Select(cell => StringProperty(cell, "case_id"))
            .Concat(missingExpectedCellIds.Select(CaseIdFromCellId))
            .Where(caseId => !string.IsNullOrWhiteSpace(caseId))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var packs = new List<JsonObject>();
        foreach (var caseId in caseIds)
        {
            packs.Add(BuildCasePack(caseId, cells, judgements, missingExpectedCellIds));
        }

        return packs;
    }

    private static JsonObject BuildCasePack(
        string caseId,
        IReadOnlyList<JsonObject> cells,
        IReadOnlyDictionary<string, JsonObject> judgements,
        IReadOnlyList<string> missingExpectedCellIds)
    {
        var caseCells = cells
            .Where(cell => StringProperty(cell, "case_id") == caseId)
            .OrderBy(CellId, StringComparer.Ordinal)
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
            ["missing_expected_cell_ids"] = StringArray(caseMissing),
            ["score_summary"] = new JsonObject
            {
                ["average"] = NullableDecimal(Average(scores)),
                ["min"] = NullableDecimal(scores.Count == 0 ? null : scores.Min()),
                ["max"] = NullableDecimal(scores.Count == 0 ? null : scores.Max()),
            },
            ["cells"] = rowCells,
        };
    }

    private static JsonObject BuildCaseCell(JsonObject cell, IReadOnlyDictionary<string, JsonObject> judgements, List<decimal> scores)
    {
        var cellId = CellId(cell);
        judgements.TryGetValue(cellId, out var judgement);
        var score = Score(judgement);
        if (score is not null)
        {
            scores.Add(score.Value);
        }

        return new JsonObject
        {
            ["cell_id"] = cellId,
            ["mode_id"] = StringProperty(cell, "mode_id"),
            ["attempt_id"] = StringProperty(cell, "attempt_id"),
            ["status"] = StringProperty(cell, "status"),
            ["score"] = NullableDecimal(score),
            ["final_answer_present"] = cell["adapter_output"]?["final_answer_present"]?.GetValue<bool>() ?? false,
            ["evidence_refs"] = CloneObject(cell["evidence_refs"]),
            ["judgement_ref"] = judgement is null ? null : $"judgements/{StringProperty(judgement, "judgement_name")}/cells/{cellId}.json",
            ["integrity_warnings"] = new JsonArray(),
        };
    }

    private static JsonArray CasePackReferences(IReadOnlyList<JsonObject> casePacks)
    {
        var references = new JsonArray();
        foreach (var pack in casePacks)
        {
            var caseId = StringProperty(pack, "case_id");
            references.Add(new JsonObject
            {
                ["case_id"] = caseId,
                ["path"] = $"reports/cases/{caseId}/case-review.json",
                ["markdown_path"] = $"reports/cases/{caseId}/case-review.md",
                ["cell_count"] = OptionalInt(pack, "cell_count", 0),
                ["missing_expected_cell_count"] = OptionalInt(pack, "missing_expected_cell_count", 0),
                ["score_summary"] = CloneObject(pack["score_summary"]),
            });
        }

        return references;
    }

    private static JsonObject ScoresByCell(IReadOnlyDictionary<string, JsonObject> judgements)
    {
        var scores = new JsonObject();
        foreach (var item in judgements.OrderBy(item => item.Key, StringComparer.Ordinal))
        {
            scores[item.Key] = NullableDecimal(Score(item.Value));
        }

        return scores;
    }

    private static decimal? Score(JsonObject? judgement)
    {
        return judgement?["quality"]?["overall_score"]?.GetValue<decimal>();
    }

    private static decimal? Average(IReadOnlyList<decimal> values)
    {
        return values.Count == 0 ? null : Math.Round(values.Sum() / values.Count, 3, MidpointRounding.AwayFromZero);
    }

    private static JsonNode? NullableDecimal(decimal? value)
    {
        return value.HasValue ? JsonValue.Create(PythonFloatDecimal(value.Value)) : null;
    }

    private static decimal PythonFloatDecimal(decimal value)
    {
        return value == decimal.Truncate(value)
            ? decimal.Parse(value.ToString("0", CultureInfo.InvariantCulture) + ".0", CultureInfo.InvariantCulture)
            : value;
    }

    private static void WriteReportFiles(string packageRoot, JsonObject report, IReadOnlyList<JsonObject> casePacks, string primaryJudgement)
    {
        var reportsRoot = Path.Combine(packageRoot, "reports");
        Directory.CreateDirectory(Path.Combine(reportsRoot, "cases"));
        WriteJson(Path.Combine(reportsRoot, "report.json"), report);
        File.WriteAllText(Path.Combine(reportsRoot, "report.md"), ReportMarkdown(report));
        foreach (var pack in casePacks)
        {
            var caseId = StringProperty(pack, "case_id");
            var caseRoot = Path.Combine(reportsRoot, "cases", caseId);
            Directory.CreateDirectory(caseRoot);
            WriteJson(Path.Combine(caseRoot, "case-review.json"), pack);
            File.WriteAllText(Path.Combine(caseRoot, "case-review.md"), CasePackMarkdown(pack));
        }

        Directory.CreateDirectory(Path.Combine(packageRoot, ".lorq"));
        WriteJson(Path.Combine(packageRoot, ".lorq", "report.json"), new JsonObject
        {
            ["schema_version"] = "lorq.report.v1alpha1",
            ["contract_version"] = ContractVersion,
            ["report"] = "reports/report.json",
            ["markdown"] = "reports/report.md",
            ["primary_judgement"] = primaryJudgement,
            ["case_count"] = casePacks.Count,
        });
    }

    private static string ReportMarkdown(JsonObject report)
    {
        var summary = report["summary"]!.AsObject();
        var package = report["package"]!.AsObject();
        var judgement = report["primary_judgement"]!.AsObject();
        var lines = new List<string>
        {
            "# LORQ package report",
            "",
            $"Package: `{StringProperty(package, "package_id")}`",
            $"Kind: `{StringProperty(package, "package_kind")}`",
            $"Primary judgement: `{StringProperty(judgement, "name")}`",
            $"Real LLM used for judgement: `{BoolText(judgement["source"]?["real_llm_used"]?.GetValue<bool>() ?? false)}`",
            "",
            "## Summary",
            "",
            $"- Cells: {OptionalInt(summary, "cell_count", 0)} present / {OptionalInt(summary, "expected_cell_count", 0)} expected",
            $"- Missing expected cells: {OptionalInt(summary, "missing_expected_cell_count", 0)}",
            $"- Integrity OK: {BoolText(summary["integrity_ok"]?.GetValue<bool>() ?? false)}",
            $"- Warning count: {OptionalInt(summary, "warning_count", 0)}",
            $"- Average score: {DecimalText(summary["score_summary"]?["overall_average"]?.GetValue<decimal>())}",
            "",
            "## Status counts",
            "",
        };
        foreach (var status in summary["status_counts"]!.AsObject())
        {
            lines.Add($"- `{status.Key}`: {status.Value?.GetValue<int>() ?? 0}");
        }

        var missingExpectedCellIds = StringArrayValues(summary["missing_expected_cell_ids"]);
        if (missingExpectedCellIds.Count > 0)
        {
            lines.AddRange(new[] { "", "## Missing expected cells", "" });
            foreach (var cellId in missingExpectedCellIds)
            {
                lines.Add($"- `{cellId}`");
            }
        }

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
            lines.Add($"| `{StringProperty(casePack, "case_id")}` | {OptionalInt(casePack, "cell_count", 0)} | {OptionalInt(casePack, "missing_expected_cell_count", 0)} | {DecimalText(casePack["score_summary"]?["average"]?.GetValue<decimal>())} |");
        }

        return string.Join('\n', lines) + "\n";
    }

    private static string CasePackMarkdown(JsonObject casePack)
    {
        var lines = new List<string>
        {
            $"# LORQ case review: {StringProperty(casePack, "case_id")}",
            "",
            $"Cells: {OptionalInt(casePack, "cell_count", 0)}",
            $"Missing expected cells: {OptionalInt(casePack, "missing_expected_cell_count", 0)}",
            $"Average score: {DecimalText(casePack["score_summary"]?["average"]?.GetValue<decimal>())}",
            "",
            "| Cell | Mode | Status | Score | Final answer |",
            "| --- | --- | --- | ---: | --- |",
        };
        foreach (var cell in casePack["cells"]!.AsArray().OfType<JsonObject>())
        {
            lines.Add($"| `{StringProperty(cell, "cell_id")}` | `{StringProperty(cell, "mode_id")}` | {StringProperty(cell, "status")} | {DecimalText(cell["score"]?.GetValue<decimal>())} | {BoolText(cell["final_answer_present"]?.GetValue<bool>() ?? false)} |");
        }

        var missingExpectedCellIds = StringArrayValues(casePack["missing_expected_cell_ids"]);
        if (missingExpectedCellIds.Count > 0)
        {
            lines.AddRange(new[] { "", "## Missing expected cells" });
            foreach (var cellId in missingExpectedCellIds)
            {
                lines.Add($"- `{cellId}`");
            }
        }

        return string.Join('\n', lines) + "\n";
    }

    private static LorqPackageReportResult SuccessfulResult(
        LorqPackageReportRequest request,
        string packageRoot,
        JsonObject report,
        IReadOnlyList<JsonObject> casePacks,
        IReadOnlyList<string> missingExpectedCellIds,
        IReadOnlyList<LorqDiagnostic> diagnostics)
    {
        return new LorqPackageReportResult(
            !diagnostics.Any(diagnostic => diagnostic.Severity == "error"),
            packageRoot,
            request.PrimaryJudgement,
            "reports/report.json",
            "reports/report.md",
            casePacks.Count,
            missingExpectedCellIds,
            ScoreSummaryAsDictionary(report["summary"]?["score_summary"] as JsonObject),
            diagnostics.ToArray());
    }

    private static LorqPackageReportResult FailedResult(LorqPackageReportRequest request, string packageRoot, IReadOnlyList<LorqDiagnostic> diagnostics)
    {
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

    private static JsonObject LoadPrimaryJudgementManifest(string packageRoot, string primaryJudgement)
    {
        var indexPath = Path.Combine(packageRoot, ".lorq", "judgements", primaryJudgement + ".json");
        if (File.Exists(indexPath))
        {
            return ReadJsonObject(indexPath);
        }

        var manifestPath = Path.Combine(packageRoot, "judgements", primaryJudgement, "judgement.manifest.json");
        if (!File.Exists(manifestPath))
        {
            throw new LorqPackageFormatException($"Missing primary LORQ judgement pass: {primaryJudgement}.");
        }

        return ReadJsonObject(manifestPath);
    }

    private static IReadOnlyDictionary<string, JsonObject> LoadPrimaryJudgements(string packageRoot, string primaryJudgement)
    {
        var cellsRoot = Path.Combine(packageRoot, "judgements", primaryJudgement, "cells");
        if (!Directory.Exists(cellsRoot))
        {
            throw new LorqPackageFormatException($"Missing judgement cells directory: {cellsRoot}.");
        }

        return Directory.EnumerateFiles(cellsRoot, "*.json")
            .Order(StringComparer.Ordinal)
            .Select(ReadJsonObject)
            .Where(judgement => !string.IsNullOrWhiteSpace(CellId(judgement)))
            .ToDictionary(CellId, StringComparer.Ordinal);
    }

    private static IReadOnlyList<JsonObject> LoadPackageCells(string packageRoot)
    {
        var cellsRoot = Path.Combine(packageRoot, ".lorq", "cells");
        if (!Directory.Exists(cellsRoot))
        {
            throw new LorqPackageFormatException($"Missing LORQ cell index: {cellsRoot}.");
        }

        return Directory.EnumerateFiles(cellsRoot, "*.json")
            .Order(StringComparer.Ordinal)
            .Select(ReadJsonObject)
            .ToArray();
    }

    private static IReadOnlyList<string> MissingExpectedCellIds(JsonObject coverage)
    {
        return StringArrayValues(coverage["missing_cells"]);
    }

    private static IReadOnlyList<string> StringArrayValues(JsonNode? node)
    {
        return node is JsonArray array
            ? array.Select(item => item?.GetValue<string>() ?? string.Empty).Where(item => item.Length > 0).ToArray()
            : Array.Empty<string>();
    }

    private static int WarningCount(JsonObject integrity)
    {
        return integrity["warnings"] is JsonArray warnings ? warnings.Count : 0;
    }

    private static int OptionalInt(JsonObject node, string property, int fallback)
    {
        return node[property]?.GetValue<int>() ?? fallback;
    }

    private static string CellId(JsonObject cell)
    {
        return StringProperty(cell, "cell_id");
    }

    private static string CaseIdFromCellId(string cellId)
    {
        var separator = cellId.IndexOf("__", StringComparison.Ordinal);
        return separator < 0 ? cellId : cellId[..separator];
    }

    private static string StringProperty(JsonObject node, string property)
    {
        return node[property]?.GetValue<string>() ?? string.Empty;
    }

    private static string DecimalText(decimal? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        var text = PythonFloatDecimal(value.Value).ToString(CultureInfo.InvariantCulture);
        return text.Contains('.', StringComparison.Ordinal) ? text : text + ".0";
    }

    private static string BoolText(bool value)
    {
        return value ? "True" : "False";
    }

    private static JsonObject CloneObject(JsonNode? node)
    {
        return node?.DeepClone() as JsonObject ?? new JsonObject();
    }

    private static JsonArray StringArray(IEnumerable<string> values)
    {
        var array = new JsonArray();
        foreach (var value in values)
        {
            array.Add(value);
        }

        return array;
    }

    private static JsonObject ReadJsonObject(string path)
    {
        return JsonNode.Parse(File.ReadAllText(path)) as JsonObject
            ?? throw new LorqPackageFormatException($"Expected JSON object in {path}.");
    }

    private static void WriteJson(string path, JsonNode node)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, node.ToJsonString(JsonWriterOptions) + Environment.NewLine);
    }
}
