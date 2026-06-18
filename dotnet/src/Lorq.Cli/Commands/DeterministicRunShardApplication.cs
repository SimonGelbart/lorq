using Lorq.Adapters.Process;
using Lorq.Core;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Lorq.Cli.Commands;

internal static class DeterministicRunShardApplication
{
    public static async ValueTask<LorqRunShardWriteResult> RunAsync(RunOptions options, CancellationToken cancellationToken)
    {
        var suiteRoot = Path.GetFullPath(options.SuiteRoot);
        var fixture = DeterministicFakeAgentFixture.Load(ResolveFromSuite(suiteRoot, options.AdapterFixturePath));
        var adapter = new DeterministicFakeFileAdapter(fixture);
        var plan = DeterministicBenchmarkShardPlan.ReadFrom(ResolveFromSuite(suiteRoot, options.BenchmarkPath), options.ShardId);
        var cells = new List<LorqRunShardCellEvidence>();

        foreach (var cell in plan.Cells)
        {
            cells.Add(await RunCellAsync(options, suiteRoot, adapter, cell, cancellationToken));
        }

        return LorqRunShardPackageWriter.Write(new LorqRunShardWriteRequest(options.PackageId, options.ShardId, options.OutputRoot, cells));
    }

    private static async ValueTask<LorqRunShardCellEvidence> RunCellAsync(
        RunOptions options,
        string suiteRoot,
        DeterministicFakeFileAdapter adapter,
        DeterministicBenchmarkCell cell,
        CancellationToken cancellationToken)
    {
        var outputRoot = Path.GetFullPath(options.OutputRoot);
        var cellId = $"{cell.CaseId}__{cell.ModeId}__attempt-{cell.Attempt:000}";
        var attemptId = $"attempt-{cell.Attempt:000}";
        var exchangeDirectory = Path.Combine(outputRoot, ".lorq", "tmp", cellId);
        var promptText = PromptText(suiteRoot, cell.CaseId);
        var request = new FileAdapterRequest(
            FileAdapterProtocol.RequestSchemaVersion,
            FileAdapterProtocol.ContractVersion,
            new FileAdapterCell(cellId, cell.CaseId, cell.ModeId, attemptId, options.ShardId),
            new FileAdapterWorkspace(suiteRoot, exchangeDirectory, Path.Combine(exchangeDirectory, "artifacts")),
            new FileAdapterTask("prompt.txt", promptText),
            new FileAdapterLimits(30000),
            new FileAdapterExpectedOutput(FileAdapterProtocol.EvidenceFileName, "answer.md"));

        Directory.CreateDirectory(exchangeDirectory);
        await File.WriteAllTextAsync(Path.Combine(exchangeDirectory, FileAdapterProtocol.RequestFileName), JsonSerializer.Serialize(request, FileAdapterJson.Options) + Environment.NewLine, cancellationToken);
        var evidence = await adapter.InvokeAsync(request, cancellationToken);
        var evidenceJson = await File.ReadAllTextAsync(Path.Combine(exchangeDirectory, FileAdapterProtocol.EvidenceFileName), cancellationToken);
        return ToRunCellEvidence(options.ShardId, cell, promptText, evidence, evidenceJson, exchangeDirectory);
    }

    private static LorqRunShardCellEvidence ToRunCellEvidence(
        string shardId,
        DeterministicBenchmarkCell cell,
        string promptText,
        FileAdapterEvidence evidence,
        string evidenceJson,
        string exchangeDirectory)
    {
        var answerPath = Path.Combine(exchangeDirectory, evidence.FinalAnswer.Path.Replace('/', Path.DirectorySeparatorChar));
        var answer = File.Exists(answerPath) ? File.ReadAllText(answerPath) : string.Empty;
        var usage = UsageObject(evidence.Usage);
        var trace = TraceArray(evidence.Trace, cell.CaseId, cell.ModeId);
        return new LorqRunShardCellEvidence(
            evidence.CellId,
            cell.CaseId,
            cell.ModeId,
            $"attempt-{cell.Attempt:000}",
            shardId,
            promptText,
            cell.Category,
            evidence.Status,
            evidence.FinalAnswer.Present,
            answer,
            promptText.Length,
            evidence.FinalAnswer.Present ? answer.Length : 0,
            AdapterObject(evidence),
            usage,
            CountsObject(evidence),
            TimingObject(evidence),
            trace,
            ArtifactsArray(evidence),
            evidence.IntegrityWarnings,
            ProcessObject(evidence),
            DiagnosticsArray(evidence),
            evidenceJson);
    }

    private static JsonObject AdapterObject(FileAdapterEvidence evidence)
    {
        return new JsonObject
        {
            ["id"] = evidence.Adapter.Id,
            ["backend"] = "deterministic-file-adapter",
            ["output_format"] = FileAdapterProtocol.EvidenceSchemaVersion,
            ["input_mode"] = "file",
            ["exit_code"] = evidence.Process.ExitCode,
            ["timed_out"] = evidence.Timing.TimedOut,
            ["ok"] = evidence.Status == "completed",
            ["error_category"] = evidence.Status == "completed" ? null : evidence.Status,
        };
    }

    private static JsonObject UsageObject(FileAdapterUsage usage)
    {
        var uncached = Math.Max(usage.InputTokens - usage.CachedInputTokens, 0);
        var total = usage.InputTokens + usage.OutputTokens;
        return new JsonObject
        {
            ["input_tokens"] = usage.InputTokens,
            ["cached_input_tokens"] = usage.CachedInputTokens,
            ["output_tokens"] = usage.OutputTokens,
            ["uncached_input_tokens"] = uncached,
            ["total_tokens"] = total,
            ["cache_hit_rate"] = usage.InputTokens == 0 ? 0 : (double)usage.CachedInputTokens / usage.InputTokens,
        };
    }

    private static JsonObject CountsObject(FileAdapterEvidence evidence)
    {
        var commandEvents = evidence.Trace.Count(item => item.Kind == "tool.command");
        return new JsonObject
        {
            ["json_events"] = evidence.Trace.Count,
            ["tool_events"] = evidence.Counts.ToolCallCount,
            ["command_events"] = commandEvents,
        };
    }

    private static JsonObject TimingObject(FileAdapterEvidence evidence)
    {
        return new JsonObject
        {
            ["elapsed_ms"] = evidence.Timing.ElapsedMilliseconds,
            ["setup_elapsed_ms"] = 0,
        };
    }

    private static JsonObject ProcessObject(FileAdapterEvidence evidence)
    {
        return new JsonObject
        {
            ["exit_code"] = evidence.Process.ExitCode,
            ["stdout_path"] = evidence.Process.StdoutPath,
            ["stderr_path"] = evidence.Process.StderrPath,
        };
    }

    private static JsonArray DiagnosticsArray(FileAdapterEvidence evidence)
    {
        var diagnostics = new JsonArray();
        foreach (var diagnostic in evidence.Diagnostics)
        {
            diagnostics.Add(new JsonObject
            {
                ["code"] = diagnostic.Code,
                ["severity"] = diagnostic.Severity,
                ["message"] = diagnostic.Message,
            });
        }

        return diagnostics;
    }

    private static JsonArray TraceArray(IReadOnlyList<FileAdapterTraceEvent> events, string caseId, string modeId)
    {
        var trace = new JsonArray();
        var index = 0;
        foreach (var item in events)
        {
            trace.Add(TraceObject(item, caseId, modeId, index));
            index++;
        }

        return trace;
    }

    private static JsonObject TraceObject(FileAdapterTraceEvent item, string caseId, string modeId, int index)
    {
        var node = new JsonObject
        {
            ["type"] = item.Kind,
            ["fixture_sequence"] = index,
        };
        if (item.Kind == "tool.command")
        {
            node["command"] = item.Message;
            node["case"] = caseId;
            node["mode"] = modeId;
        }
        else
        {
            node["text"] = item.Message;
        }

        return node;
    }

    private static JsonArray ArtifactsArray(FileAdapterEvidence evidence)
    {
        var artifacts = new JsonArray();
        foreach (var artifact in evidence.Artifacts)
        {
            artifacts.Add(new JsonObject
            {
                ["path"] = artifact.Path,
                ["kind"] = artifact.Kind,
                ["sha256"] = artifact.Sha256,
            });
        }

        return artifacts;
    }

    private static string PromptText(string suiteRoot, string caseId)
    {
        var task = CaseTask(Path.Combine(suiteRoot, "cases", caseId + ".yaml"));
        return "You are evaluating repository evidence. Use only the provided repository context." + Environment.NewLine + Environment.NewLine + "Task:" + Environment.NewLine + task;
    }

    private static string CaseTask(string casePath)
    {
        var lines = File.ReadAllLines(casePath);
        var task = new List<string>();
        var inside = false;
        foreach (var rawLine in lines)
        {
            if (rawLine == "task: |")
            {
                inside = true;
                continue;
            }

            if (inside && rawLine.Length > 0 && !char.IsWhiteSpace(rawLine[0]))
            {
                break;
            }

            if (inside)
            {
                task.Add(rawLine.StartsWith("  ", StringComparison.Ordinal) ? rawLine[2..] : rawLine);
            }
        }

        return string.Join(Environment.NewLine, task).TrimEnd() + Environment.NewLine;
    }

    private static string ResolveFromSuite(string suiteRoot, string path)
    {
        return Path.IsPathRooted(path) ? path : Path.Combine(suiteRoot, path);
    }
}
