using System.Text.Json;

namespace Lorq.Adapters.Process;

/// <summary>
/// Performs shallow JSON contract checks for the file adapter protocol before typed model binding.
/// </summary>
public sealed class FileAdapterProtocolJsonValidator
{
    public void ValidateRequestJson(string json)
    {
        using var document = Parse(json, "LORQ-ADAPTER-REQUEST-INVALID");
        var root = document.RootElement;
        RequireString(root, "schema_version", FileAdapterProtocol.RequestSchemaVersion, "LORQ-ADAPTER-REQUEST-SCHEMA");
        RequireString(root, "contract_version", FileAdapterProtocol.ContractVersion, "LORQ-ADAPTER-REQUEST-CONTRACT");
        ValidateRequestCell(RequireObject(root, "cell", "LORQ-ADAPTER-REQUEST-CELL"));
        ValidateRequestWorkspace(RequireObject(root, "workspace", "LORQ-ADAPTER-REQUEST-WORKSPACE"));
        ValidateRequestTask(RequireObject(root, "task", "LORQ-ADAPTER-REQUEST-TASK"));
        ValidateRequestLimits(RequireObject(root, "limits", "LORQ-ADAPTER-REQUEST-LIMITS"));
        ValidateRequestExpectedOutput(RequireObject(root, "expected_output", "LORQ-ADAPTER-REQUEST-EXPECTED-OUTPUT"));
    }

    public void ValidateEvidenceJson(string json, string expectedCellId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedCellId);
        using var document = Parse(json, "LORQ-ADAPTER-EVIDENCE-INVALID");
        var root = document.RootElement;
        RequireString(root, "schema_version", FileAdapterProtocol.EvidenceSchemaVersion, "LORQ-ADAPTER-EVIDENCE-SCHEMA");
        RequireString(root, "contract_version", FileAdapterProtocol.ContractVersion, "LORQ-ADAPTER-EVIDENCE-CONTRACT");
        RequireString(root, "cell_id", expectedCellId, "LORQ-ADAPTER-EVIDENCE-CELL");
        ValidateAdapter(RequireObject(root, "adapter", "LORQ-ADAPTER-EVIDENCE-ADAPTER"));
        RequireAnyString(root, "status", "LORQ-ADAPTER-EVIDENCE-STATUS");
        ValidateFinalAnswer(RequireObject(root, "final_answer", "LORQ-ADAPTER-EVIDENCE-FINAL-ANSWER"));
        ValidateUsage(RequireObject(root, "usage", "LORQ-ADAPTER-EVIDENCE-USAGE"));
        ValidateCounts(RequireObject(root, "counts", "LORQ-ADAPTER-EVIDENCE-COUNTS"));
        ValidateTiming(RequireObject(root, "timing", "LORQ-ADAPTER-EVIDENCE-TIMING"));
        ValidateProcess(RequireObject(root, "process", "LORQ-ADAPTER-EVIDENCE-PROCESS"));
        RequireArray(root, "trace", "LORQ-ADAPTER-EVIDENCE-TRACE");
        RequireArray(root, "artifacts", "LORQ-ADAPTER-EVIDENCE-ARTIFACTS");
        RequireArray(root, "integrity_warnings", "LORQ-ADAPTER-EVIDENCE-WARNINGS");
        RequireArray(root, "diagnostics", "LORQ-ADAPTER-EVIDENCE-DIAGNOSTICS");
    }

    private static JsonDocument Parse(string json, string code)
    {
        try
        {
            return JsonDocument.Parse(json);
        }
        catch (JsonException exception)
        {
            throw new FileAdapterProtocolException(code, exception.Message);
        }
    }

    private static void ValidateRequestCell(JsonElement cell)
    {
        RequireAnyString(cell, "cell_id", "LORQ-ADAPTER-REQUEST-CELL");
        RequireAnyString(cell, "case_id", "LORQ-ADAPTER-REQUEST-CELL");
        RequireAnyString(cell, "mode_id", "LORQ-ADAPTER-REQUEST-CELL");
        RequireAnyString(cell, "attempt_id", "LORQ-ADAPTER-REQUEST-CELL");
        RequireAnyString(cell, "shard_id", "LORQ-ADAPTER-REQUEST-CELL");
    }

    private static void ValidateRequestWorkspace(JsonElement workspace)
    {
        RequireAnyString(workspace, "root", "LORQ-ADAPTER-REQUEST-WORKSPACE");
        RequireAnyString(workspace, "evidence_directory", "LORQ-ADAPTER-REQUEST-WORKSPACE");
        RequireAnyString(workspace, "artifacts_directory", "LORQ-ADAPTER-REQUEST-WORKSPACE");
    }

    private static void ValidateRequestTask(JsonElement task)
    {
        RequireAnyString(task, "prompt_path", "LORQ-ADAPTER-REQUEST-TASK");
        RequireAnyString(task, "prompt_text", "LORQ-ADAPTER-REQUEST-TASK");
    }

    private static void ValidateRequestLimits(JsonElement limits)
    {
        var timeout = RequireNumber(limits, "timeout_milliseconds", "LORQ-ADAPTER-REQUEST-LIMITS");
        Require(timeout > 0, "LORQ-ADAPTER-REQUEST-LIMITS", "The adapter request timeout_milliseconds must be greater than zero.");
    }

    private static void ValidateRequestExpectedOutput(JsonElement expectedOutput)
    {
        RequireAnyString(expectedOutput, "evidence_path", "LORQ-ADAPTER-REQUEST-EXPECTED-OUTPUT");
        RequireAnyString(expectedOutput, "final_answer_path", "LORQ-ADAPTER-REQUEST-EXPECTED-OUTPUT");
    }

    private static void ValidateAdapter(JsonElement adapter)
    {
        RequireAnyString(adapter, "id", "LORQ-ADAPTER-EVIDENCE-ADAPTER");
        RequireAnyString(adapter, "kind", "LORQ-ADAPTER-EVIDENCE-ADAPTER");
        RequireAnyString(adapter, "version", "LORQ-ADAPTER-EVIDENCE-ADAPTER");
    }

    private static void ValidateFinalAnswer(JsonElement finalAnswer)
    {
        RequireBool(finalAnswer, "present", "LORQ-ADAPTER-EVIDENCE-FINAL-ANSWER");
        RequireAnyString(finalAnswer, "path", "LORQ-ADAPTER-EVIDENCE-FINAL-ANSWER");
    }

    private static void ValidateUsage(JsonElement usage)
    {
        RequireNumber(usage, "input_tokens", "LORQ-ADAPTER-EVIDENCE-USAGE");
        RequireNumber(usage, "cached_input_tokens", "LORQ-ADAPTER-EVIDENCE-USAGE");
        RequireNumber(usage, "output_tokens", "LORQ-ADAPTER-EVIDENCE-USAGE");
        RequireNumber(usage, "reasoning_output_tokens", "LORQ-ADAPTER-EVIDENCE-USAGE");
        RequireNumber(usage, "estimated_cost_usd", "LORQ-ADAPTER-EVIDENCE-USAGE");
    }

    private static void ValidateCounts(JsonElement counts)
    {
        RequireNumber(counts, "tool_call_count", "LORQ-ADAPTER-EVIDENCE-COUNTS");
        RequireNumber(counts, "artifact_count", "LORQ-ADAPTER-EVIDENCE-COUNTS");
        RequireNumber(counts, "trace_event_count", "LORQ-ADAPTER-EVIDENCE-COUNTS");
    }

    private static void ValidateTiming(JsonElement timing)
    {
        RequireNumber(timing, "elapsed_milliseconds", "LORQ-ADAPTER-EVIDENCE-TIMING");
        RequireBool(timing, "timed_out", "LORQ-ADAPTER-EVIDENCE-TIMING");
    }

    private static void ValidateProcess(JsonElement process)
    {
        RequireNumber(process, "exit_code", "LORQ-ADAPTER-EVIDENCE-PROCESS");
        RequireAnyString(process, "stdout_path", "LORQ-ADAPTER-EVIDENCE-PROCESS");
        RequireAnyString(process, "stderr_path", "LORQ-ADAPTER-EVIDENCE-PROCESS");
    }

    private static JsonElement RequireObject(JsonElement root, string propertyName, string code)
    {
        Require(root.TryGetProperty(propertyName, out var value), code, $"The JSON contract must include {propertyName}.");
        Require(value.ValueKind == JsonValueKind.Object, code, $"The JSON contract property {propertyName} must be an object.");
        return value;
    }

    private static JsonElement RequireArray(JsonElement root, string propertyName, string code)
    {
        Require(root.TryGetProperty(propertyName, out var value), code, $"The JSON contract must include {propertyName}.");
        Require(value.ValueKind == JsonValueKind.Array, code, $"The JSON contract property {propertyName} must be an array.");
        return value;
    }

    private static string RequireAnyString(JsonElement root, string propertyName, string code)
    {
        Require(root.TryGetProperty(propertyName, out var value), code, $"The JSON contract must include {propertyName}.");
        Require(value.ValueKind == JsonValueKind.String, code, $"The JSON contract property {propertyName} must be a string.");
        var text = value.GetString();
        Require(!string.IsNullOrWhiteSpace(text), code, $"The JSON contract property {propertyName} must not be empty.");
        return text!;
    }

    private static string RequireString(JsonElement root, string propertyName, string expected, string code)
    {
        var actual = RequireAnyString(root, propertyName, code);
        Require(string.Equals(actual, expected, StringComparison.Ordinal), code, $"The JSON contract property {propertyName} is not supported.");
        return actual;
    }

    private static decimal RequireNumber(JsonElement root, string propertyName, string code)
    {
        Require(root.TryGetProperty(propertyName, out var value), code, $"The JSON contract must include {propertyName}.");
        Require(value.ValueKind == JsonValueKind.Number, code, $"The JSON contract property {propertyName} must be numeric.");
        return value.GetDecimal();
    }

    private static bool RequireBool(JsonElement root, string propertyName, string code)
    {
        Require(root.TryGetProperty(propertyName, out var value), code, $"The JSON contract must include {propertyName}.");
        Require(value.ValueKind is JsonValueKind.True or JsonValueKind.False, code, $"The JSON contract property {propertyName} must be boolean.");
        return value.GetBoolean();
    }

    private static void Require(bool condition, string code, string message)
    {
        if (!condition)
        {
            throw new FileAdapterProtocolException(code, message);
        }
    }
}
