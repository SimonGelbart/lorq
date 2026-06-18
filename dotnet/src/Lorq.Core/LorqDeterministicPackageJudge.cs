using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Lorq.Core;

/// <summary>
/// Attaches a deterministic, fixture-backed judgement pass to a merged LORQ package.
/// </summary>
public static class LorqDeterministicPackageJudge
{
    private const string ContractVersion = "lorq.contract.v1alpha1";
    private const string Backend = "deterministic-fake";

    private static readonly JsonSerializerOptions JsonWriterOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static LorqPackageJudgementResult Attach(LorqPackageJudgeRequest request)
    {
        var packageRoot = Path.GetFullPath(request.PackageRoot);
        var fixturePath = Path.GetFullPath(request.FixturePath);
        var diagnostics = new List<LorqDiagnostic>();

        try
        {
            var validation = LorqPackageValidator.Validate(packageRoot);
            diagnostics.AddRange(validation.Diagnostics);
            if (request.Strict && !validation.Ok)
            {
                return FailedResult(request, packageRoot, diagnostics);
            }

            var fixture = FakeJudgeFixture.Load(fixturePath);
            var cells = LoadCells(packageRoot);
            var cellJudgements = new List<JsonObject>();
            var missingFixtureCellIds = new List<string>();
            foreach (var cell in cells.OrderBy(CellId, StringComparer.Ordinal))
            {
                AddCellJudgement(request, fixturePath, fixture, cell, cellJudgements, missingFixtureCellIds);
            }

            if (request.Strict && missingFixtureCellIds.Count > 0)
            {
                diagnostics.Add(new LorqDiagnostic("LORQ310", "error", "Missing deterministic judgement fixture entries.", packageRoot));
                return BuildResult(request, packageRoot, cells.Count, cellJudgements, missingFixtureCellIds, MissingExpectedCellIds(packageRoot), diagnostics, ok: false);
            }

            var missingExpectedCellIds = MissingExpectedCellIds(packageRoot);
            WriteJudgementFiles(request, packageRoot, fixturePath, fixture, cells.Count, cellJudgements, missingFixtureCellIds, missingExpectedCellIds);
            return BuildResult(request, packageRoot, cells.Count, cellJudgements, missingFixtureCellIds, missingExpectedCellIds, diagnostics, ok: missingFixtureCellIds.Count == 0);
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

    public static LorqPackageJudgementResult Attach(
        string packageRoot,
        string judgeName,
        string fixturePath,
        bool strict = true)
    {
        return Attach(new LorqPackageJudgeRequest(packageRoot, judgeName, fixturePath, strict));
    }

    private static IReadOnlyList<JsonObject> LoadCells(string packageRoot)
    {
        var cellsRoot = Path.Combine(packageRoot, ".lorq", "cells");
        if (!Directory.Exists(cellsRoot))
        {
            throw new LorqPackageFormatException($"Package is missing .lorq/cells: {packageRoot}.");
        }

        return Directory
            .EnumerateFiles(cellsRoot, "*.json")
            .Order(StringComparer.Ordinal)
            .Select(ReadJsonObject)
            .ToArray();
    }

    private static void AddCellJudgement(
        LorqPackageJudgeRequest request,
        string fixturePath,
        FakeJudgeFixture fixture,
        JsonObject cell,
        List<JsonObject> cellJudgements,
        List<string> missingFixtureCellIds)
    {
        var cellId = CellId(cell);
        if (!fixture.TryGetPayload(CellFixtureKeys(cell), out var fixtureKey, out var payload))
        {
            missingFixtureCellIds.Add(cellId);
            return;
        }

        cellJudgements.Add(BuildCellJudgement(request, fixturePath, fixture, fixtureKey, payload, cell));
    }

    private static JsonObject BuildCellJudgement(
        LorqPackageJudgeRequest request,
        string fixturePath,
        FakeJudgeFixture fixture,
        string fixtureKey,
        FakeJudgePayload payload,
        JsonObject cell)
    {
        return new JsonObject
        {
            ["schema_version"] = "lorq.cell-judgement.v1alpha1",
            ["contract_version"] = ContractVersion,
            ["judgement_name"] = request.JudgeName,
            ["cell_id"] = CellId(cell),
            ["case_id"] = StringProperty(cell, "case_id"),
            ["mode_id"] = StringProperty(cell, "mode_id"),
            ["attempt_id"] = StringProperty(cell, "attempt_id"),
            ["cell_status"] = StringProperty(cell, "status"),
            ["status"] = "judged",
            ["source"] = new JsonObject
            {
                ["backend"] = Backend,
                ["fixture_schema_version"] = fixture.SchemaVersion,
                ["fixture_file"] = Path.GetFileName(fixturePath),
                ["fixture_key"] = fixtureKey,
                ["real_llm_used"] = false,
            },
            ["quality"] = payload.ToQualityJson(),
            ["input_refs"] = InputRefs(cell),
        };
    }

    private static void WriteJudgementFiles(
        LorqPackageJudgeRequest request,
        string packageRoot,
        string fixturePath,
        FakeJudgeFixture fixture,
        int cellCount,
        IReadOnlyList<JsonObject> cellJudgements,
        IReadOnlyList<string> missingFixtureCellIds,
        IReadOnlyList<string> missingExpectedCellIds)
    {
        var judgementRoot = Path.Combine(packageRoot, "judgements", request.JudgeName);
        var cellRoot = Path.Combine(judgementRoot, "cells");
        RecreateDirectory(judgementRoot);
        Directory.CreateDirectory(cellRoot);
        Directory.CreateDirectory(Path.Combine(packageRoot, ".lorq", "judgements"));

        foreach (var judgement in cellJudgements)
        {
            WriteJson(Path.Combine(cellRoot, CellId(judgement) + ".json"), judgement);
        }

        var scoreSummary = ScoreSummary(cellJudgements);
        var manifest = BuildManifest(request, fixturePath, fixture, cellCount, cellJudgements, missingFixtureCellIds, missingExpectedCellIds, scoreSummary);
        WriteJson(Path.Combine(judgementRoot, "judgement.manifest.json"), manifest);
        WriteJson(Path.Combine(judgementRoot, "judgement.summary.json"), BuildSummary(request, cellCount, cellJudgements.Count, missingFixtureCellIds, missingExpectedCellIds, scoreSummary));
        WriteJson(Path.Combine(packageRoot, ".lorq", "judgements", request.JudgeName + ".json"), manifest);
    }

    private static JsonObject BuildManifest(
        LorqPackageJudgeRequest request,
        string fixturePath,
        FakeJudgeFixture fixture,
        int cellCount,
        IReadOnlyList<JsonObject> cellJudgements,
        IReadOnlyList<string> missingFixtureCellIds,
        IReadOnlyList<string> missingExpectedCellIds,
        JsonObject scoreSummary)
    {
        return new JsonObject
        {
            ["schema_version"] = "lorq.judgement-pass.v1alpha1",
            ["contract_version"] = ContractVersion,
            ["judgement_name"] = request.JudgeName,
            ["package_root"] = ".",
            ["backend"] = Backend,
            ["source"] = new JsonObject
            {
                ["fixture_schema_version"] = fixture.SchemaVersion,
                ["fixture_file"] = Path.GetFileName(fixturePath),
                ["real_llm_used"] = false,
            },
            ["cell_count"] = cellCount,
            ["judged_cell_count"] = cellJudgements.Count,
            ["missing_fixture_cell_ids"] = StringArray(missingFixtureCellIds),
            ["missing_expected_cell_ids"] = StringArray(missingExpectedCellIds),
            ["score_summary"] = scoreSummary.DeepClone(),
            ["cell_judgement_refs"] = CellJudgementRefs(request.JudgeName, cellJudgements),
        };
    }

    private static JsonObject BuildSummary(
        LorqPackageJudgeRequest request,
        int cellCount,
        int judgedCellCount,
        IReadOnlyList<string> missingFixtureCellIds,
        IReadOnlyList<string> missingExpectedCellIds,
        JsonObject scoreSummary)
    {
        return new JsonObject
        {
            ["schema_version"] = "lorq.judgement-summary.v1alpha1",
            ["contract_version"] = ContractVersion,
            ["judgement_name"] = request.JudgeName,
            ["cell_count"] = cellCount,
            ["judged_cell_count"] = judgedCellCount,
            ["missing_fixture_cell_count"] = missingFixtureCellIds.Count,
            ["missing_expected_cell_count"] = missingExpectedCellIds.Count,
            ["score_summary"] = scoreSummary.DeepClone(),
        };
    }

    private static JsonArray CellJudgementRefs(string judgeName, IReadOnlyList<JsonObject> cellJudgements)
    {
        var refs = new JsonArray();
        foreach (var judgement in cellJudgements)
        {
            var cellId = CellId(judgement);
            refs.Add(new JsonObject
            {
                ["cell_id"] = cellId,
                ["path"] = $"judgements/{judgeName}/cells/{cellId}.json",
            });
        }

        return refs;
    }

    private static JsonObject ScoreSummary(IReadOnlyList<JsonObject> cellJudgements)
    {
        var scores = new List<decimal>();
        var byMode = new Dictionary<string, List<decimal>>(StringComparer.Ordinal);
        var byCase = new Dictionary<string, List<decimal>>(StringComparer.Ordinal);
        foreach (var judgement in cellJudgements)
        {
            var score = judgement["quality"]?["overall_score"]?.GetValue<decimal>();
            if (score is null)
            {
                continue;
            }

            scores.Add(score.Value);
            AddScore(byMode, StringProperty(judgement, "mode_id"), score.Value);
            AddScore(byCase, StringProperty(judgement, "case_id"), score.Value);
        }

        return new JsonObject
        {
            ["overall_average"] = NullableDecimal(Average(scores)),
            ["overall_min"] = NullableDecimal(scores.Count == 0 ? null : scores.Min()),
            ["overall_max"] = NullableDecimal(scores.Count == 0 ? null : scores.Max()),
            ["by_mode"] = GroupAverages(byMode),
            ["by_case"] = GroupAverages(byCase),
        };
    }

    private static void AddScore(Dictionary<string, List<decimal>> groups, string key, decimal score)
    {
        if (!groups.TryGetValue(key, out var scores))
        {
            scores = new List<decimal>();
            groups[key] = scores;
        }

        scores.Add(score);
    }

    private static JsonObject GroupAverages(IReadOnlyDictionary<string, List<decimal>> groups)
    {
        var result = new JsonObject();
        foreach (var group in groups.OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            result[group.Key] = NullableDecimal(Average(group.Value));
        }

        return result;
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
            ? decimal.Parse(value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + ".0", System.Globalization.CultureInfo.InvariantCulture)
            : value;
    }

    private static JsonObject InputRefs(JsonObject cell)
    {
        var evidenceRefs = cell["evidence_refs"] as JsonObject ?? new JsonObject();
        return new JsonObject
        {
            ["cell_result"] = JsonString(evidenceRefs, "cell_result"),
            ["cell_dir"] = JsonString(evidenceRefs, "cell_dir"),
            ["final_answer"] = JsonString(evidenceRefs, "final_answer"),
            ["validation"] = JsonString(evidenceRefs, "validation"),
            ["trace"] = JsonString(evidenceRefs, "trace"),
        };
    }

    private static IReadOnlyList<string> MissingExpectedCellIds(string packageRoot)
    {
        var path = Path.Combine(packageRoot, ".lorq", "coverage.json");
        using var document = JsonHelpers.ReadDocument(path);
        return JsonHelpers.OptionalStringArray(document.RootElement, "missing_cells").Select(item => item).ToArray();
    }

    private static IEnumerable<string> CellFixtureKeys(JsonObject cell)
    {
        var caseId = StringProperty(cell, "case_id");
        var modeId = StringProperty(cell, "mode_id");
        var attemptId = StringProperty(cell, "attempt_id");
        var attemptNumber = AttemptNumber(attemptId);
        if (attemptNumber is not null)
        {
            yield return $"{caseId}|{modeId}|{attemptNumber}";
        }

        yield return $"{caseId}|{modeId}|{attemptId}";
        if (attemptNumber != 1)
        {
            yield return $"{caseId}|{modeId}|1";
        }
    }

    private static int? AttemptNumber(string attemptId)
    {
        var text = attemptId.StartsWith("attempt-", StringComparison.Ordinal) ? attemptId[8..] : attemptId;
        return int.TryParse(text, out var parsed) ? parsed : null;
    }

    private static LorqPackageJudgementResult BuildResult(
        LorqPackageJudgeRequest request,
        string packageRoot,
        int cellCount,
        IReadOnlyList<JsonObject> cellJudgements,
        IReadOnlyList<string> missingFixtureCellIds,
        IReadOnlyList<string> missingExpectedCellIds,
        IReadOnlyList<LorqDiagnostic> diagnostics,
        bool ok)
    {
        return new LorqPackageJudgementResult(
            ok,
            packageRoot,
            request.JudgeName,
            Backend,
            cellCount,
            cellJudgements.Count,
            missingFixtureCellIds.ToArray(),
            missingExpectedCellIds.ToArray(),
            ScoreSummaryAsDictionary(ScoreSummary(cellJudgements)),
            diagnostics.ToArray());
    }

    private static LorqPackageJudgementResult FailedResult(LorqPackageJudgeRequest request, string packageRoot, IReadOnlyList<LorqDiagnostic> diagnostics)
    {
        return new LorqPackageJudgementResult(
            false,
            packageRoot,
            request.JudgeName,
            Backend,
            0,
            0,
            Array.Empty<string>(),
            Array.Empty<string>(),
            new Dictionary<string, object?>(),
            diagnostics.ToArray());
    }

    private static IReadOnlyDictionary<string, object?> ScoreSummaryAsDictionary(JsonObject scoreSummary)
    {
        return JsonSerializer.Deserialize<Dictionary<string, object?>>(scoreSummary.ToJsonString()) ?? new Dictionary<string, object?>();
    }

    private static string CellId(JsonObject cell)
    {
        return StringProperty(cell, "cell_id");
    }

    private static string StringProperty(JsonObject node, string property)
    {
        return node[property]?.GetValue<string>() ?? string.Empty;
    }

    private static string? JsonString(JsonObject node, string property)
    {
        return node[property]?.GetValue<string>();
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

    private static void RecreateDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }

        Directory.CreateDirectory(path);
    }
}
