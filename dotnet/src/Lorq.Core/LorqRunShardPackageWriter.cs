using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Lorq.Core;

/// <summary>
/// Writes deterministic v1-alpha run-shard packages from adapter evidence.
/// </summary>
public static class LorqRunShardPackageWriter
{
    private const string ContractVersion = "lorq.contract.v1alpha1";

    private static readonly JsonSerializerOptions JsonWriterOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static LorqRunShardWriteResult Write(LorqRunShardWriteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var diagnostics = new List<LorqDiagnostic>();
        var outputRoot = Path.GetFullPath(request.OutputRoot);

        try
        {
            RecreateDirectory(outputRoot);
            WritePackageSkeleton(request, outputRoot);
            WriteCells(request, outputRoot);
            WriteShardManifest(request, outputRoot);
            WriteMergeLog(request, outputRoot);
            var expectedCellIds = request.Cells.Select(cell => cell.CellId).Order(StringComparer.Ordinal).ToArray();
            var rebuild = LorqPackageIndexRebuilder.Rebuild(outputRoot, outputRoot, new LorqIndexRebuildOptions(expectedCellIds, Array.Empty<JsonObject>()));
            diagnostics.AddRange(rebuild.Diagnostics);
            return new LorqRunShardWriteResult(!diagnostics.Any(diagnostic => diagnostic.Severity == "error"), outputRoot, request.PackageId, request.ShardId, request.Cells.Count, diagnostics);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or LorqPackageFormatException)
        {
            diagnostics.Add(new LorqDiagnostic("LORQ400", "error", exception.Message, outputRoot));
            return new LorqRunShardWriteResult(false, outputRoot, request.PackageId, request.ShardId, 0, diagnostics);
        }
    }

    private static void WritePackageSkeleton(LorqRunShardWriteRequest request, string outputRoot)
    {
        Directory.CreateDirectory(Path.Combine(outputRoot, "runs", request.ShardId, "cells"));
        Directory.CreateDirectory(Path.Combine(outputRoot, "judgements"));
        Directory.CreateDirectory(Path.Combine(outputRoot, "reports", "cases"));
        Directory.CreateDirectory(Path.Combine(outputRoot, ".lorq"));
        WriteExperimentYaml(request, outputRoot);
    }

    private static void WriteCells(LorqRunShardWriteRequest request, string outputRoot)
    {
        foreach (var cell in request.Cells.OrderBy(cell => cell.CellId, StringComparer.Ordinal))
        {
            WriteCell(outputRoot, cell);
        }
    }

    private static void WriteCell(string outputRoot, LorqRunShardCellEvidence cell)
    {
        var cellRelativeDirectory = CellRelativeDirectory(cell);
        var cellDirectory = Path.Combine(outputRoot, cellRelativeDirectory.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(cellDirectory);

        File.WriteAllText(Path.Combine(cellDirectory, "prompt.txt"), cell.PromptText);
        File.WriteAllText(Path.Combine(cellDirectory, "answer.md"), cell.FinalAnswer);
        File.WriteAllText(Path.Combine(cellDirectory, "stderr.txt"), string.Empty);
        WriteJson(Path.Combine(cellDirectory, "validation.json"), Validation(cell));
        WriteJson(Path.Combine(cellDirectory, "events.summary.json"), EventsSummary(cell));
        WriteJson(Path.Combine(cellDirectory, "active-skills.json"), ActiveSkills());
        File.WriteAllText(Path.Combine(cellDirectory, "events.normalized.jsonl"), NormalizedEvents(cell));
        File.WriteAllText(Path.Combine(cellDirectory, "stdout.raw.jsonl"), RawEvents(cell));
        File.WriteAllText(Path.Combine(cellDirectory, "stdout.raw.txt"), RawEvents(cell));
        File.WriteAllText(Path.Combine(cellDirectory, "adapter.evidence.json"), cell.AdapterEvidenceJson.EndsWith(Environment.NewLine, StringComparison.Ordinal) ? cell.AdapterEvidenceJson : cell.AdapterEvidenceJson + Environment.NewLine);
        WriteJson(Path.Combine(cellDirectory, "cell_result.json"), CellResult(cell));
    }

    private static JsonObject CellResult(LorqRunShardCellEvidence cell)
    {
        return new JsonObject
        {
            ["schema_version"] = "lorq.cell-evidence.v1alpha1",
            ["contract_version"] = ContractVersion,
            ["cell_id"] = cell.CellId,
            ["case_id"] = cell.CaseId,
            ["mode_id"] = cell.ModeId,
            ["attempt_id"] = cell.AttemptId,
            ["shard_id"] = cell.ShardId,
            ["prompt_style"] = "neutral",
            ["category"] = cell.Category,
            ["status"] = cell.Status,
            ["source"] = Source(),
            ["fingerprint"] = Fingerprint(),
            ["adapter_output"] = AdapterOutput(cell),
            ["evidence_refs"] = EvidenceRefs(cell),
        };
    }

    private static JsonObject Source()
    {
        return new JsonObject
        {
            ["implementation"] = "dotnet-v1",
            ["dotnet_package"] = "lorq",
            ["dotnet_version"] = "v1alpha1",
            ["source_schema_version"] = "lorq.file-adapter-evidence.v1alpha1",
        };
    }

    private static JsonObject Fingerprint()
    {
        return new JsonObject
        {
            ["repo"] = "fake_project",
            ["repo_type"] = "local",
            ["ref"] = "HEAD",
            ["commit"] = "07e6cdc7b3e58436088c73c984f6d38b2d1fc18a",
            ["dirty"] = true,
            ["is_git_repo"] = true,
        };
    }

    private static JsonObject AdapterOutput(LorqRunShardCellEvidence cell)
    {
        return new JsonObject
        {
            ["status"] = cell.Status,
            ["final_answer_present"] = cell.FinalAnswerPresent,
            ["final_answer_chars"] = cell.FinalAnswerChars,
            ["prompt_chars"] = cell.PromptChars,
            ["adapter"] = CloneObject(cell.Adapter),
            ["usage"] = CloneObject(cell.Usage),
            ["counts"] = CloneObject(cell.Counts),
            ["timing"] = CloneObject(cell.Timing),
            ["trace"] = new JsonObject
            {
                ["summary"] = EventsSummary(cell),
                ["normalized_events_path"] = "events.normalized.jsonl",
            },
            ["validation"] = ValidationSummary(cell),
            ["artifacts"] = ArtifactRefs(cell),
        };
    }

    private static JsonObject EvidenceRefs(LorqRunShardCellEvidence cell)
    {
        var cellDirectory = CellRelativeDirectory(cell);
        return new JsonObject
        {
            ["cell_dir"] = cellDirectory,
            ["final_answer"] = cellDirectory + "/answer.md",
            ["cell_result"] = cellDirectory + "/cell_result.json",
            ["prompt"] = cellDirectory + "/prompt.txt",
            ["validation"] = cellDirectory + "/validation.json",
            ["trace"] = cellDirectory + "/events.normalized.jsonl",
        };
    }

    private static JsonArray ArtifactRefs(LorqRunShardCellEvidence cell)
    {
        var cellDirectory = CellRelativeDirectory(cell);
        var refs = new JsonArray
        {
            ArtifactRef(cellDirectory, "prompt.txt"),
            ArtifactRef(cellDirectory, "answer.md"),
            ArtifactRef(cellDirectory, "validation.json"),
            ArtifactRef(cellDirectory, "events.normalized.jsonl"),
            ArtifactRef(cellDirectory, "events.summary.json"),
            ArtifactRef(cellDirectory, "stdout.raw.jsonl"),
            ArtifactRef(cellDirectory, "stdout.raw.txt"),
            ArtifactRef(cellDirectory, "stderr.txt"),
            ArtifactRef(cellDirectory, "active-skills.json"),
            ArtifactRef(cellDirectory, "adapter.evidence.json"),
        };
        return refs;
    }

    private static JsonObject ArtifactRef(string cellDirectory, string fileName)
    {
        return new JsonObject
        {
            ["path"] = cellDirectory + "/" + fileName,
            ["kind"] = fileName,
        };
    }

    private static JsonObject Validation(LorqRunShardCellEvidence cell)
    {
        return new JsonObject
        {
            ["case_id"] = cell.CaseId,
            ["mode_id"] = cell.ModeId,
            ["ok"] = cell.FinalAnswerPresent,
            ["hard_passed"] = cell.FinalAnswerPresent ? 1 : 0,
            ["hard_total"] = 1,
            ["soft_passed"] = 0,
            ["soft_total"] = 0,
            ["checks"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "answer_not_empty",
                    ["ok"] = cell.FinalAnswerPresent,
                    ["message"] = "answer.md should contain a non-empty final answer",
                },
            },
            ["schema_version"] = "agent-eval.validation-result.v1",
            ["contract_version"] = "agent-eval.contract.v1",
        };
    }

    private static JsonObject ValidationSummary(LorqRunShardCellEvidence cell)
    {
        return new JsonObject
        {
            ["ok"] = cell.FinalAnswerPresent,
            ["hard_passed"] = cell.FinalAnswerPresent ? 1 : 0,
            ["hard_total"] = 1,
            ["soft_passed"] = 0,
            ["soft_total"] = 0,
        };
    }

    private static JsonObject EventsSummary(LorqRunShardCellEvidence cell)
    {
        var counts = new JsonObject();
        foreach (var item in cell.Trace.OfType<JsonObject>())
        {
            var type = item["type"]?.GetValue<string>() ?? item["kind"]?.GetValue<string>() ?? "event";
            counts[type] = (counts[type]?.GetValue<int>() ?? 0) + 1;
        }

        return new JsonObject
        {
            ["event_count"] = cell.Trace.Count,
            ["event_type_counts"] = counts,
            ["backends"] = new JsonArray(),
            ["usage"] = new JsonObject(),
            ["schema_version"] = "agent-eval.event-summary.v1",
            ["contract_version"] = "agent-eval.contract.v1",
        };
    }

    private static JsonObject ActiveSkills()
    {
        return new JsonObject
        {
            ["schema_version"] = "agent-eval.active-skills.v1",
            ["skills"] = new JsonArray(),
            ["skill_count"] = 0,
        };
    }

    private static string NormalizedEvents(LorqRunShardCellEvidence cell)
    {
        var lines = new List<string>();
        var index = 0;
        foreach (var item in cell.Trace.OfType<JsonObject>())
        {
            var normalized = CloneObject(item);
            var type = normalized["type"]?.GetValue<string>() ?? normalized["kind"]?.GetValue<string>() ?? "event";
            normalized["schema_version"] = "agent-eval.normalized-event.v1";
            normalized["event_index"] = index;
            normalized["timestamp_ms"] = (cell.Timing["elapsed_ms"]?.GetValue<int>() ?? 0) + index;
            normalized["event_type"] = type;
            normalized["source"] = "deterministic-fake-file-adapter";
            lines.Add(JsonSerializer.Serialize(normalized));
            index++;
        }

        return string.Join(Environment.NewLine, lines) + (lines.Count == 0 ? string.Empty : Environment.NewLine);
    }

    private static string RawEvents(LorqRunShardCellEvidence cell)
    {
        var lines = cell.Trace.OfType<JsonObject>().Select(item => JsonSerializer.Serialize(item));
        return string.Join(Environment.NewLine, lines) + (cell.Trace.Count == 0 ? string.Empty : Environment.NewLine);
    }

    private static void WriteShardManifest(LorqRunShardWriteRequest request, string outputRoot)
    {
        var cellIds = request.Cells.Select(cell => cell.CellId).Order(StringComparer.Ordinal).ToArray();
        var cellArray = new JsonArray();
        foreach (var cellId in cellIds)
        {
            cellArray.Add(cellId);
        }

        WriteJson(Path.Combine(outputRoot, "runs", request.ShardId, "shard.manifest.json"), new JsonObject
        {
            ["schema_version"] = "lorq.run-shard-manifest.v1alpha1",
            ["contract_version"] = ContractVersion,
            ["shard_id"] = request.ShardId,
            ["cell_count"] = cellIds.Length,
            ["cell_ids"] = cellArray,
            ["source"] = new JsonObject
            {
                ["kind"] = "dotnet-file-adapter-run",
                ["label"] = request.ShardId,
            },
        });
    }

    private static void WriteMergeLog(LorqRunShardWriteRequest request, string outputRoot)
    {
        WriteJson(Path.Combine(outputRoot, ".lorq", "merge-log.json"), new JsonObject
        {
            ["schema_version"] = "lorq.merge-log.v1alpha1",
            ["contract_version"] = ContractVersion,
            ["operation"] = "dotnet-v1-run-shard-export",
            ["inputs"] = new JsonArray
            {
                new JsonObject
                {
                    ["kind"] = "file-adapter-results",
                    ["label"] = request.ShardId,
                },
            },
            ["outputs"] = new JsonArray
            {
                new JsonObject
                {
                    ["kind"] = "lorq-run-shard",
                    ["package_id"] = request.PackageId,
                    ["shard_id"] = request.ShardId,
                },
            },
            ["cell_count"] = request.Cells.Count,
        });
    }

    private static void WriteExperimentYaml(LorqRunShardWriteRequest request, string outputRoot)
    {
        var text = $"""
package_schema_version: 1
package_kind: run_shard
package_id: {request.PackageId}
created_by:
  name: lorq dotnet-v1
  implementation: dotnet
  version: v1alpha1
shards:
  - {request.ShardId}
cell_count: {request.Cells.Count}
""";
        File.WriteAllText(Path.Combine(outputRoot, "experiment.yaml"), text + Environment.NewLine);
    }

    private static void WriteJson(string path, JsonNode node)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, node.ToJsonString(JsonWriterOptions) + Environment.NewLine);
    }

    private static string CellRelativeDirectory(LorqRunShardCellEvidence cell)
    {
        return $"runs/{cell.ShardId}/cells/{cell.CellId}";
    }

    private static JsonObject CloneObject(JsonObject node)
    {
        return node.DeepClone() as JsonObject ?? new JsonObject();
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
