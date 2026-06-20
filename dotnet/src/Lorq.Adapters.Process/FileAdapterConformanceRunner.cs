using System.Text.Json;

namespace Lorq.Adapters.Process;

/// <summary>
/// Runs deterministic probes against a one-shot file adapter command.
/// </summary>
public sealed class FileAdapterConformanceRunner
{
    private const string ScenarioName = "basic-exchange";

    public async ValueTask<FileAdapterConformanceReport> RunAsync(
        FileAdapterProcessCommand command,
        string outputRoot,
        int timeoutMilliseconds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputRoot);
        if (timeoutMilliseconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(timeoutMilliseconds), "The timeout must be greater than zero milliseconds.");
        }

        Directory.CreateDirectory(outputRoot);
        var exchangeRoot = Path.Combine(outputRoot, ScenarioName);
        if (Directory.Exists(exchangeRoot))
        {
            Directory.Delete(exchangeRoot, recursive: true);
        }

        Directory.CreateDirectory(exchangeRoot);
        var request = CreateRequest(exchangeRoot, timeoutMilliseconds);
        await WritePromptAsync(request, cancellationToken).ConfigureAwait(false);
        var result = await RunScenarioAsync(command, request, cancellationToken).ConfigureAwait(false);
        return CreateReport(result);
    }

    private static async ValueTask<FileAdapterConformanceScenarioResult> RunScenarioAsync(
        FileAdapterProcessCommand command,
        FileAdapterRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var evidence = await new ExternalFileAdapterProcess(command).InvokeAsync(request, cancellationToken).ConfigureAwait(false);
            return PassedResult(request, evidence);
        }
        catch (FileAdapterProtocolException exception)
        {
            return FailedResult(request, exception.Code, exception.Message);
        }
        catch (JsonException exception)
        {
            return FailedResult(request, "LORQ-ADAPTER-EVIDENCE-INVALID", exception.Message);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return FailedResult(request, "LORQ-ADAPTER-CONFORMANCE-IO", exception.Message);
        }
    }

    private static FileAdapterConformanceScenarioResult PassedResult(FileAdapterRequest request, FileAdapterEvidence evidence)
    {
        var observations = new List<string>
        {
            "adapter-request.json was written",
            "adapter-evidence.json was read",
        };

        observations.AddRange(ValidateExchangeFiles(request, evidence));
        if (observations.Any(observation => observation.StartsWith("missing ", StringComparison.Ordinal)))
        {
            return new FileAdapterConformanceScenarioResult(
                ScenarioName,
                false,
                "LORQ-ADAPTER-CONFORMANCE-FILES",
                "The adapter evidence references files that were not written in the exchange directory.",
                request.Workspace.EvidenceDirectory,
                evidence.Adapter.Id,
                observations);
        }

        return new FileAdapterConformanceScenarioResult(
            ScenarioName,
            true,
            null,
            null,
            request.Workspace.EvidenceDirectory,
            evidence.Adapter.Id,
            observations);
    }

    private static IEnumerable<string> ValidateExchangeFiles(FileAdapterRequest request, FileAdapterEvidence evidence)
    {
        yield return ExistingObservation(request.Workspace.EvidenceDirectory, "adapter-process.stdout.txt", "adapter process stdout capture");
        yield return ExistingObservation(request.Workspace.EvidenceDirectory, "adapter-process.stderr.txt", "adapter process stderr capture");
        yield return ExistingObservation(request.Workspace.EvidenceDirectory, request.ExpectedOutput.FinalAnswerPath, "final answer");
        yield return ExistingObservation(request.Workspace.EvidenceDirectory, evidence.Process.StdoutPath, "evidence stdout capture");
        yield return ExistingObservation(request.Workspace.EvidenceDirectory, evidence.Process.StderrPath, "evidence stderr capture");
    }

    private static string ExistingObservation(string root, string relativePath, string name)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return "missing " + name + " path";
        }

        return File.Exists(Path.Combine(root, relativePath))
            ? name + " file exists"
            : "missing " + name + " file";
    }

    private static FileAdapterConformanceScenarioResult FailedResult(FileAdapterRequest request, string code, string message)
    {
        return new FileAdapterConformanceScenarioResult(
            ScenarioName,
            false,
            code,
            message,
            request.Workspace.EvidenceDirectory,
            null,
            new[] { "adapter command did not complete a valid protocol exchange" });
    }

    private static FileAdapterConformanceReport CreateReport(FileAdapterConformanceScenarioResult result)
    {
        return new FileAdapterConformanceReport(
            result.Passed,
            FileAdapterProtocol.ContractVersion,
            FileAdapterProtocol.RequestSchemaVersion,
            FileAdapterProtocol.EvidenceSchemaVersion,
            new[] { result });
    }

    private static FileAdapterRequest CreateRequest(string exchangeRoot, int timeoutMilliseconds)
    {
        var workspaceRoot = Path.Combine(exchangeRoot, "workspace");
        var evidenceRoot = Path.Combine(exchangeRoot, "exchange");
        var artifactsRoot = Path.Combine(evidenceRoot, "artifacts");
        Directory.CreateDirectory(workspaceRoot);
        Directory.CreateDirectory(evidenceRoot);
        Directory.CreateDirectory(artifactsRoot);

        return new FileAdapterRequest(
            FileAdapterProtocol.RequestSchemaVersion,
            FileAdapterProtocol.ContractVersion,
            new FileAdapterCell("conformance-basic__baseline__attempt-001", "conformance-basic", "baseline", "attempt-001", "shard-001"),
            new FileAdapterWorkspace(workspaceRoot, evidenceRoot, artifactsRoot),
            new FileAdapterTask("prompt.txt", "Produce a deterministic one-line answer for a LORQ file adapter conformance probe."),
            new FileAdapterLimits(timeoutMilliseconds),
            new FileAdapterExpectedOutput(FileAdapterProtocol.EvidenceFileName, "answer.md"));
    }

    private static async ValueTask WritePromptAsync(FileAdapterRequest request, CancellationToken cancellationToken)
    {
        var promptPath = Path.Combine(request.Workspace.Root, request.Task.PromptPath);
        await File.WriteAllTextAsync(promptPath, request.Task.PromptText + Environment.NewLine, cancellationToken).ConfigureAwait(false);
    }
}
