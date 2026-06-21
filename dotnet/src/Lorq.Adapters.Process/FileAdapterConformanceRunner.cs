namespace Lorq.Adapters.Process;

/// <summary>
/// Runs deterministic probes against a one-shot file adapter command.
/// </summary>
public sealed class FileAdapterConformanceRunner
{
    private static readonly ConformanceScenario[] Scenarios =
    {
        new("basic-exchange", "conformance-basic", "Produce a deterministic one-line answer for a LORQ file adapter conformance probe."),
        new("metadata-capture", "conformance-metadata", "Produce an answer and complete usage, timing, trace, and process metadata."),
        new("artifact-reference", "conformance-artifact", "Produce an answer with valid artifact references relative to the exchange directory."),
    };

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
        var results = new List<FileAdapterConformanceScenarioResult>();
        foreach (var scenario in Scenarios)
        {
            var exchangeRoot = PrepareScenarioDirectory(outputRoot, scenario.Name);
            var request = CreateRequest(exchangeRoot, scenario, timeoutMilliseconds);
            await WritePromptAsync(request, cancellationToken).ConfigureAwait(false);
            results.Add(await RunScenarioAsync(command, scenario.Name, request, cancellationToken).ConfigureAwait(false));
        }

        return CreateReport(results);
    }

    private static string PrepareScenarioDirectory(string outputRoot, string scenarioName)
    {
        var exchangeRoot = Path.Combine(outputRoot, scenarioName);
        if (Directory.Exists(exchangeRoot))
        {
            Directory.Delete(exchangeRoot, recursive: true);
        }

        Directory.CreateDirectory(exchangeRoot);
        return exchangeRoot;
    }

    private static async ValueTask<FileAdapterConformanceScenarioResult> RunScenarioAsync(
        FileAdapterProcessCommand command,
        string scenarioName,
        FileAdapterRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var evidence = await new ExternalFileAdapterProcess(command).InvokeAsync(request, cancellationToken).ConfigureAwait(false);
            return PassedResult(scenarioName, request, evidence);
        }
        catch (FileAdapterProtocolException exception)
        {
            return FailedResult(scenarioName, request, exception.Code, exception.Message);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return FailedResult(scenarioName, request, "LORQ-ADAPTER-CONFORMANCE-IO", exception.Message);
        }
    }

    private static FileAdapterConformanceScenarioResult PassedResult(string scenarioName, FileAdapterRequest request, FileAdapterEvidence evidence)
    {
        var observations = new List<string>
        {
            "adapter-request.json was written",
            "adapter-evidence.json was read",
        };

        observations.AddRange(ValidateExchangeFiles(request, evidence));
        observations.AddRange(ValidateEvidenceMetadata(evidence));
        observations.AddRange(ValidateArtifactReferences(request, evidence));
        if (observations.Any(observation => observation.StartsWith("missing ", StringComparison.Ordinal)))
        {
            return new FileAdapterConformanceScenarioResult(
                scenarioName,
                false,
                "LORQ-ADAPTER-CONFORMANCE-FILES",
                "The adapter evidence references files or metadata that were not written in the exchange directory.",
                request.Workspace.EvidenceDirectory,
                evidence.Adapter.Id,
                observations);
        }

        return new FileAdapterConformanceScenarioResult(
            scenarioName,
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

    private static IEnumerable<string> ValidateEvidenceMetadata(FileAdapterEvidence evidence)
    {
        yield return evidence.Usage.InputTokens >= 0 && evidence.Usage.OutputTokens >= 0 && evidence.Usage.EstimatedCostUsd >= 0
            ? "usage metadata is complete"
            : "missing usage metadata";
        yield return evidence.Timing.ElapsedMilliseconds >= 0
            ? "timing metadata is complete"
            : "missing timing metadata";
        yield return evidence.Trace.Count > 0
            ? "trace output is present"
            : "missing trace output";
    }

    private static IEnumerable<string> ValidateArtifactReferences(FileAdapterRequest request, FileAdapterEvidence evidence)
    {
        foreach (var artifact in evidence.Artifacts)
        {
            yield return ExistingObservation(request.Workspace.EvidenceDirectory, artifact.Path, "artifact " + artifact.Kind);
        }
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

    private static FileAdapterConformanceScenarioResult FailedResult(string scenarioName, FileAdapterRequest request, string code, string message)
    {
        return new FileAdapterConformanceScenarioResult(
            scenarioName,
            false,
            code,
            message,
            request.Workspace.EvidenceDirectory,
            null,
            new[] { "adapter command did not complete a valid protocol exchange" });
    }

    private static FileAdapterConformanceReport CreateReport(IReadOnlyList<FileAdapterConformanceScenarioResult> results)
    {
        return new FileAdapterConformanceReport(
            results.All(result => result.Passed),
            FileAdapterProtocol.ContractVersion,
            FileAdapterProtocol.RequestSchemaVersion,
            FileAdapterProtocol.EvidenceSchemaVersion,
            results);
    }

    private static FileAdapterRequest CreateRequest(string exchangeRoot, ConformanceScenario scenario, int timeoutMilliseconds)
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
            new FileAdapterCell(scenario.CaseId + "__baseline__attempt-001", scenario.CaseId, "baseline", "attempt-001", "shard-001"),
            new FileAdapterWorkspace(workspaceRoot, evidenceRoot, artifactsRoot),
            new FileAdapterTask("prompt.txt", scenario.PromptText),
            new FileAdapterLimits(timeoutMilliseconds),
            new FileAdapterExpectedOutput(FileAdapterProtocol.EvidenceFileName, "answer.md"));
    }

    private static async ValueTask WritePromptAsync(FileAdapterRequest request, CancellationToken cancellationToken)
    {
        var promptPath = Path.Combine(request.Workspace.Root, request.Task.PromptPath);
        await File.WriteAllTextAsync(promptPath, request.Task.PromptText + Environment.NewLine, cancellationToken).ConfigureAwait(false);
    }

    private sealed record ConformanceScenario(string Name, string CaseId, string PromptText);
}
