using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Lorq.Core;

/// <summary>
/// Rebuilds deterministic v1-alpha .lorq indexes from package evidence.
/// </summary>
public static class LorqPackageIndexRebuilder
{
    private const string ContractVersion = "lorq.contract.v1alpha1";

    private static readonly JsonSerializerOptions JsonWriterOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static LorqIndexRebuildResult Rebuild(string packageRoot, string targetRoot)
    {
        var sourceRoot = Path.GetFullPath(packageRoot);
        var destinationRoot = Path.GetFullPath(targetRoot);
        var diagnostics = new List<LorqDiagnostic>();

        if (!Directory.Exists(sourceRoot))
        {
            diagnostics.Add(new LorqDiagnostic("LORQ001", "error", "Package root does not exist.", sourceRoot));
            return new LorqIndexRebuildResult(false, destinationRoot, Array.Empty<string>(), diagnostics);
        }

        var packageKind = ReadPackageKind(sourceRoot);
        var orderedCells = LoadCellsInManifestOrder(sourceRoot, packageKind);
        var expectedCellIds = LoadExpectedCellIds(sourceRoot, orderedCells);
        var generatedFiles = new List<string>();
        var indexRoot = Path.Combine(destinationRoot, ".lorq");

        RecreateDirectory(indexRoot);
        WriteCellIndexes(indexRoot, orderedCells, generatedFiles);
        WriteJson(indexRoot, "coverage.json", BuildCoverage(orderedCells, expectedCellIds), generatedFiles);
        WriteJson(indexRoot, "fingerprints.json", BuildFingerprints(orderedCells), generatedFiles);
        WriteJson(indexRoot, "integrity.json", BuildIntegrity(sourceRoot, orderedCells, expectedCellIds), generatedFiles);
        WriteMergeLogIndex(sourceRoot, indexRoot, generatedFiles);
        WriteJudgementIndexes(sourceRoot, indexRoot, generatedFiles);
        WriteReportIndex(sourceRoot, indexRoot, generatedFiles);

        return new LorqIndexRebuildResult(true, destinationRoot, generatedFiles.Order(StringComparer.Ordinal).ToArray(), diagnostics);
    }

    private static string ReadPackageKind(string packageRoot)
    {
        var manifest = YamlLite.ParseTopLevel(Path.Combine(packageRoot, "experiment.yaml"));
        return YamlLite.RequiredString(manifest, "package_kind", Path.Combine(packageRoot, "experiment.yaml"));
    }

    private static IReadOnlyList<JsonObject> LoadCellsInManifestOrder(string packageRoot, string packageKind)
    {
        var cells = new List<JsonObject>();
        var manifestPaths = Directory
            .EnumerateFiles(Path.Combine(packageRoot, "runs"), "shard.manifest.json", SearchOption.AllDirectories)
            .Order(StringComparer.Ordinal);

        foreach (var manifestPath in manifestPaths)
        {
            using var manifest = JsonHelpers.ReadDocument(manifestPath);
            var shardId = JsonHelpers.RequiredString(manifest.RootElement, "shard_id", manifestPath);
            var cellIds = JsonHelpers.OptionalStringArray(manifest.RootElement, "cell_ids");
            foreach (var cellId in cellIds)
            {
                cells.Add(ReadCellResult(packageRoot, packageKind, shardId, cellId));
            }
        }

        return cells;
    }

    private static JsonObject ReadCellResult(string packageRoot, string packageKind, string shardId, string cellId)
    {
        var path = Path.Combine(packageRoot, "runs", shardId, "cells", cellId, "cell_result.json");
        var cell = ReadJsonObject(path);
        if (packageKind == "merged_experiment")
        {
            cell["source_shard_id"] = shardId;
        }

        return cell;
    }

    private static IReadOnlyList<string> LoadExpectedCellIds(string packageRoot, IReadOnlyList<JsonObject> cells)
    {
        var coveragePath = Path.Combine(packageRoot, ".lorq", "coverage.json");
        if (!File.Exists(coveragePath))
        {
            return cells.Select(CellId).Order(StringComparer.Ordinal).ToArray();
        }

        using var coverage = JsonHelpers.ReadDocument(coveragePath);
        var expected = JsonHelpers.OptionalStringArray(coverage.RootElement, "expected_cell_ids");
        return expected.Count == 0
            ? cells.Select(CellId).Order(StringComparer.Ordinal).ToArray()
            : expected.Order(StringComparer.Ordinal).ToArray();
    }

    private static void WriteCellIndexes(string indexRoot, IReadOnlyList<JsonObject> cells, List<string> generatedFiles)
    {
        var cellsRoot = Path.Combine(indexRoot, "cells");
        Directory.CreateDirectory(cellsRoot);
        foreach (var cell in cells.OrderBy(CellId, StringComparer.Ordinal))
        {
            WriteJson(indexRoot, Path.Combine("cells", CellId(cell) + ".json"), cell, generatedFiles);
        }
    }

    private static JsonObject BuildCoverage(IReadOnlyList<JsonObject> cells, IReadOnlyList<string> expectedCellIds)
    {
        var presentCellIds = cells.Select(CellId).Order(StringComparer.Ordinal).ToArray();
        var presentCellSet = presentCellIds.ToHashSet(StringComparer.Ordinal);
        var statusCounts = StatusCounts(cells);

        return new JsonObject
        {
            ["schema_version"] = "lorq.coverage.v1alpha1",
            ["contract_version"] = ContractVersion,
            ["cell_count"] = cells.Count,
            ["expected_cell_count"] = expectedCellIds.Count,
            ["cases"] = StringArray(cells.Select(StringProperty("case_id")).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)),
            ["modes"] = StringArray(cells.Select(StringProperty("mode_id")).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)),
            ["attempts"] = StringArray(cells.Select(StringProperty("attempt_id")).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)),
            ["present_cell_ids"] = StringArray(presentCellIds),
            ["expected_cell_ids"] = StringArray(expectedCellIds),
            ["status_counts"] = statusCounts,
            ["missing_cells"] = StringArray(expectedCellIds.Where(cellId => !presentCellSet.Contains(cellId))),
            ["skipped_cells"] = StringArray(cells.Where(cell => StringProperty("status")(cell) == "skipped").Select(CellId)),
        };
    }

    private static JsonObject StatusCounts(IReadOnlyList<JsonObject> cells)
    {
        var counts = new JsonObject();
        foreach (var status in cells.Select(StringProperty("status")))
        {
            var count = counts[status]?.GetValue<int>() ?? 0;
            counts[status] = count + 1;
        }

        return counts;
    }

    private static JsonObject BuildFingerprints(IReadOnlyList<JsonObject> cells)
    {
        var byCell = new JsonObject();
        var unique = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (var cell in cells)
        {
            var fingerprint = CloneObject(cell["fingerprint"]);
            var cellId = CellId(cell);
            byCell[cellId] = fingerprint.DeepClone();
            var canonicalKey = CanonicalJson(fingerprint);
            if (!unique.TryGetValue(canonicalKey, out var cellIds))
            {
                cellIds = new List<string>();
                unique[canonicalKey] = cellIds;
            }

            cellIds.Add(cellId);
        }

        var uniqueFingerprints = new JsonArray();
        foreach (var item in unique.OrderBy(item => item.Key, StringComparer.Ordinal))
        {
            uniqueFingerprints.Add(new JsonObject
            {
                ["fingerprint"] = SortObject(ReadJsonObjectFromText(item.Key)),
                ["cell_ids"] = StringArray(item.Value.Order(StringComparer.Ordinal)),
            });
        }

        return new JsonObject
        {
            ["schema_version"] = "lorq.fingerprints.v1alpha1",
            ["contract_version"] = ContractVersion,
            ["by_cell"] = byCell,
            ["unique_fingerprint_count"] = unique.Count,
            ["unique_fingerprints"] = uniqueFingerprints,
        };
    }

    private static JsonObject BuildIntegrity(string packageRoot, IReadOnlyList<JsonObject> cells, IReadOnlyList<string> expectedCellIds)
    {
        var warnings = new JsonArray();
        var presentCellIds = cells.Select(CellId).ToHashSet(StringComparer.Ordinal);

        foreach (var cell in cells)
        {
            AddCellIntegrityWarnings(packageRoot, cell, warnings);
        }

        foreach (var missingCellId in expectedCellIds.Where(cellId => !presentCellIds.Contains(cellId)))
        {
            warnings.Add(new JsonObject
            {
                ["type"] = "missing_expected_cell",
                ["cell_id"] = missingCellId,
                ["severity"] = "warning",
            });
        }

        foreach (var sourceWarning in ExistingSourceShardWarnings(packageRoot))
        {
            warnings.Add(sourceWarning);
        }

        return new JsonObject
        {
            ["schema_version"] = "lorq.integrity.v1alpha1",
            ["contract_version"] = ContractVersion,
            ["ok"] = !warnings.OfType<JsonObject>().Any(IsErrorWarning),
            ["warnings"] = warnings,
        };
    }

    private static void AddCellIntegrityWarnings(string packageRoot, JsonObject cell, JsonArray warnings)
    {
        var cellId = CellId(cell);
        if (!FinalAnswerPresent(cell))
        {
            warnings.Add(new JsonObject
            {
                ["type"] = "missing_final_answer",
                ["cell_id"] = cellId,
                ["severity"] = "warning",
            });
        }

        var status = StringProperty("status")(cell);
        if (status is not ("completed" or "skipped"))
        {
            warnings.Add(new JsonObject
            {
                ["type"] = "non_completed_cell",
                ["cell_id"] = cellId,
                ["status"] = status,
                ["severity"] = "warning",
            });
        }

        foreach (var message in AdapterIntegrityWarnings(packageRoot, cell))
        {
            warnings.Add(new JsonObject
            {
                ["type"] = "adapter_integrity_warning",
                ["cell_id"] = cellId,
                ["message"] = message,
                ["severity"] = "warning",
            });
        }
    }

    private static IEnumerable<string> AdapterIntegrityWarnings(string packageRoot, JsonObject cell)
    {
        var cellDirectory = cell["evidence_refs"]?["cell_dir"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(cellDirectory))
        {
            yield break;
        }

        var adapterEvidencePath = Path.Combine(packageRoot, cellDirectory.Replace('/', Path.DirectorySeparatorChar), "adapter.evidence.json");
        if (!File.Exists(adapterEvidencePath))
        {
            yield break;
        }

        var adapterEvidence = ReadJsonObject(adapterEvidencePath);
        var warnings = adapterEvidence["integrity_warnings"];
        if (warnings is JsonValue value && value.TryGetValue<string>(out var warning))
        {
            yield return warning;
        }

        if (warnings is not JsonArray array)
        {
            yield break;
        }

        foreach (var item in array)
        {
            var message = item?.GetValue<string>();
            if (!string.IsNullOrWhiteSpace(message))
            {
                yield return message;
            }
        }
    }

    private static IEnumerable<JsonObject> ExistingSourceShardWarnings(string packageRoot)
    {
        var path = Path.Combine(packageRoot, ".lorq", "integrity.json");
        if (!File.Exists(path))
        {
            yield break;
        }

        var integrity = ReadJsonObject(path);
        if (integrity["warnings"] is not JsonArray warnings)
        {
            yield break;
        }

        foreach (var warning in warnings.OfType<JsonObject>())
        {
            if (warning.ContainsKey("source_shard"))
            {
                yield return CloneObject(warning);
            }
        }
    }

    private static bool IsErrorWarning(JsonObject warning)
    {
        return warning["severity"]?.GetValue<string>() == "error";
    }


    private static void WriteMergeLogIndex(string packageRoot, string indexRoot, List<string> generatedFiles)
    {
        var mergeLogPath = Path.Combine(packageRoot, ".lorq", "merge-log.json");
        if (!File.Exists(mergeLogPath))
        {
            return;
        }

        WriteJson(indexRoot, "merge-log.json", ReadJsonObject(mergeLogPath), generatedFiles);
    }

    private static void WriteJudgementIndexes(string packageRoot, string indexRoot, List<string> generatedFiles)
    {
        var judgementsRoot = Path.Combine(packageRoot, "judgements");
        if (!Directory.Exists(judgementsRoot))
        {
            return;
        }

        foreach (var manifestPath in Directory.EnumerateFiles(judgementsRoot, "judgement.manifest.json", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            var judgement = ReadJsonObject(manifestPath);
            var judgementName = judgement["judgement_name"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(judgementName))
            {
                continue;
            }

            WriteJson(indexRoot, Path.Combine("judgements", judgementName + ".json"), judgement, generatedFiles);
        }
    }

    private static void WriteReportIndex(string packageRoot, string indexRoot, List<string> generatedFiles)
    {
        var reportJsonPath = Path.Combine(packageRoot, "reports", "report.json");
        var reportMarkdownPath = Path.Combine(packageRoot, "reports", "report.md");
        if (!File.Exists(reportJsonPath) || !File.Exists(reportMarkdownPath))
        {
            return;
        }

        var report = ReadJsonObject(reportJsonPath);
        var judgementName = report["primary_judgement"]?["name"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(judgementName))
        {
            return;
        }

        var caseCount = report["case_packs"] is JsonArray casePacks ? casePacks.Count : 0;
        WriteJson(indexRoot, "report.json", new JsonObject
        {
            ["schema_version"] = "lorq.report.v1alpha1",
            ["contract_version"] = ContractVersion,
            ["report"] = "reports/report.json",
            ["markdown"] = "reports/report.md",
            ["primary_judgement"] = judgementName,
            ["case_count"] = caseCount,
        }, generatedFiles);
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

    private static Func<JsonObject, string> StringProperty(string propertyName)
    {
        return node => node[propertyName]?.GetValue<string>() ?? string.Empty;
    }

    private static string CellId(JsonObject cell)
    {
        return StringProperty("cell_id")(cell);
    }

    private static bool FinalAnswerPresent(JsonObject cell)
    {
        return cell["adapter_output"]?["final_answer_present"]?.GetValue<bool>() ?? false;
    }

    private static JsonObject CloneObject(JsonNode? node)
    {
        return node?.DeepClone() as JsonObject ?? new JsonObject();
    }

    private static JsonObject SortObject(JsonObject node)
    {
        var sorted = new JsonObject();
        foreach (var property in node.OrderBy(property => property.Key, StringComparer.Ordinal))
        {
            sorted[property.Key] = SortNode(property.Value);
        }

        return sorted;
    }

    private static JsonNode? SortNode(JsonNode? node)
    {
        return node switch
        {
            JsonObject jsonObject => SortObject(jsonObject),
            JsonArray jsonArray => SortArray(jsonArray),
            _ => node?.DeepClone(),
        };
    }

    private static JsonArray SortArray(JsonArray array)
    {
        var sorted = new JsonArray();
        foreach (var item in array)
        {
            sorted.Add(SortNode(item));
        }

        return sorted;
    }

    private static string CanonicalJson(JsonObject node)
    {
        return SortObject(node).ToJsonString(new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        });
    }

    private static JsonObject ReadJsonObjectFromText(string text)
    {
        return JsonNode.Parse(text) as JsonObject ?? new JsonObject();
    }

    private static JsonObject ReadJsonObject(string path)
    {
        return JsonNode.Parse(File.ReadAllText(path)) as JsonObject
            ?? throw new LorqPackageFormatException($"Expected JSON object in {path}.");
    }

    private static void WriteJson(string root, string relativePath, JsonNode node, List<string> generatedFiles)
    {
        var fullPath = Path.Combine(root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, node.ToJsonString(JsonWriterOptions) + Environment.NewLine);
        generatedFiles.Add(relativePath.Replace(Path.DirectorySeparatorChar, '/'));
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
