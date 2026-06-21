using Lorq.Adapters.Process;
using Lorq.Core;
using System.Text.Json.Nodes;

namespace Lorq.Cli.Runtime;

internal sealed class RunCellEvidenceFactory
{
    public LorqRunShardCellEvidence Create(
        string shardId,
        DeterministicBenchmarkCell cell,
        string promptText,
        FileAdapterEvidence evidence,
        string evidenceJson,
        string exchangeDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shardId);
        ArgumentNullException.ThrowIfNull(cell);
        ArgumentNullException.ThrowIfNull(promptText);
        ArgumentNullException.ThrowIfNull(evidence);
        ArgumentNullException.ThrowIfNull(evidenceJson);
        ArgumentException.ThrowIfNullOrWhiteSpace(exchangeDirectory);

        var answerPath = Path.Combine(exchangeDirectory, evidence.FinalAnswer.Path.Replace('/', Path.DirectorySeparatorChar));
        var answer = File.Exists(answerPath) ? File.ReadAllText(answerPath) : string.Empty;
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
            UsageObject(evidence.Usage),
            CountsObject(evidence),
            TimingObject(evidence),
            TraceArray(evidence.Trace, cell.CaseId, cell.ModeId),
            ArtifactsArray(evidence),
            evidence.IntegrityWarnings,
            ProcessObject(evidence),
            DiagnosticsArray(evidence),
            evidenceJson);
    }

    private static JsonObject AdapterObject(FileAdapterEvidence evidence)
    {
        var adapter = new JsonObject
        {
            ["id"] = evidence.Adapter.Id,
            ["kind"] = evidence.Adapter.Kind,
            ["version"] = evidence.Adapter.Version,
            ["backend"] = evidence.Adapter.Runtime?.Runtime ?? evidence.Adapter.Kind,
            ["output_format"] = evidence.Adapter.Runtime?.OutputFormat ?? FileAdapterProtocol.EvidenceSchemaVersion,
            ["input_mode"] = "file",
            ["exit_code"] = evidence.Process.ExitCode,
            ["timed_out"] = evidence.Timing.TimedOut,
            ["ok"] = evidence.Status == "completed",
            ["error_category"] = evidence.Status == "completed" ? null : evidence.Status,
        };
        if (evidence.Adapter.Runtime is not null)
        {
            adapter["runtime"] = RuntimeObject(evidence.Adapter.Runtime);
        }

        return adapter;
    }

    private static JsonObject RuntimeObject(FileAdapterRuntimeMetadata runtime)
    {
        var node = new JsonObject
        {
            ["provider"] = runtime.Provider,
            ["runtime"] = runtime.Runtime,
            ["runtime_version"] = runtime.RuntimeVersion,
            ["profile"] = runtime.Profile,
            ["command"] = runtime.Command,
            ["permission_profile"] = runtime.PermissionProfile,
            ["output_format"] = runtime.OutputFormat,
        };
        var extensions = new JsonObject();
        foreach (var item in runtime.Extensions)
        {
            extensions[item.Key] = item.Value;
        }

        node["extensions"] = extensions;
        return node;
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
            return node;
        }

        node["text"] = item.Message;
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
}
