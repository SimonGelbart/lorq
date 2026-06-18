using System.Diagnostics;
using System.Text.Json;

namespace Lorq.Adapters.Process;

/// <summary>
/// Invokes an external one-shot adapter using the LORQ file adapter protocol.
/// </summary>
public sealed class ExternalFileAdapterProcess : IFileAdapter
{
    private readonly FileAdapterProcessCommand command;

    public ExternalFileAdapterProcess(FileAdapterProcessCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        this.command = command;
    }

    public async ValueTask<FileAdapterEvidence> InvokeAsync(FileAdapterRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await PrepareExchangeAsync(request, cancellationToken).ConfigureAwait(false);
        await RunProcessAsync(request, cancellationToken).ConfigureAwait(false);
        return await ReadEvidenceAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private static async ValueTask PrepareExchangeAsync(FileAdapterRequest request, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(request.Workspace.EvidenceDirectory);
        Directory.CreateDirectory(request.Workspace.ArtifactsDirectory);
        var requestPath = RequestPath(request);
        var requestJson = JsonSerializer.Serialize(request, FileAdapterJson.Options) + Environment.NewLine;
        await File.WriteAllTextAsync(requestPath, requestJson, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask RunProcessAsync(FileAdapterRequest request, CancellationToken cancellationToken)
    {
        using var process = BuildProcess(request);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMilliseconds(request.Limits.TimeoutMilliseconds));
        Start(process);
        var stdout = process.StandardOutput.ReadToEndAsync(timeout.Token);
        var stderr = process.StandardError.ReadToEndAsync(timeout.Token);
        await WaitForExitAsync(process, timeout.Token).ConfigureAwait(false);
        await PersistProcessLogsAsync(request, await stdout.ConfigureAwait(false), await stderr.ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
        EnsureEvidenceOrSuccessfulExit(request, process.ExitCode);
    }

    private System.Diagnostics.Process BuildProcess(FileAdapterRequest request)
    {
        var startInfo = new ProcessStartInfo(command.Executable)
        {
            WorkingDirectory = WorkingDirectory(request),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        foreach (var argument in command.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        startInfo.Environment["LORQ_ADAPTER_REQUEST"] = RequestPath(request);
        startInfo.Environment["LORQ_ADAPTER_EVIDENCE"] = EvidencePath(request);
        startInfo.Environment["LORQ_ADAPTER_EXCHANGE_DIR"] = request.Workspace.EvidenceDirectory;
        startInfo.Environment["LORQ_ADAPTER_WORKSPACE_ROOT"] = request.Workspace.Root;
        return new System.Diagnostics.Process { StartInfo = startInfo };
    }

    private string WorkingDirectory(FileAdapterRequest request)
    {
        if (!string.IsNullOrWhiteSpace(command.WorkingDirectory))
        {
            return command.WorkingDirectory;
        }

        return request.Workspace.Root;
    }

    private static void Start(System.Diagnostics.Process process)
    {
        try
        {
            if (!process.Start())
            {
                throw new FileAdapterProtocolException("LORQ-ADAPTER-PROCESS-START", "The external adapter process did not start.");
            }
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            throw new FileAdapterProtocolException("LORQ-ADAPTER-PROCESS-START", exception.Message);
        }
    }

    private static async ValueTask WaitForExitAsync(System.Diagnostics.Process process, CancellationToken cancellationToken)
    {
        try
        {
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!process.HasExited)
        {
            KillProcessTree(process);
            throw new FileAdapterProtocolException("LORQ-ADAPTER-PROCESS-TIMEOUT", "The external adapter process exceeded the request timeout.");
        }
    }

    private static async ValueTask PersistProcessLogsAsync(FileAdapterRequest request, string stdout, string stderr, CancellationToken cancellationToken)
    {
        await File.WriteAllTextAsync(Path.Combine(request.Workspace.EvidenceDirectory, "adapter-process.stdout.txt"), stdout, cancellationToken).ConfigureAwait(false);
        await File.WriteAllTextAsync(Path.Combine(request.Workspace.EvidenceDirectory, "adapter-process.stderr.txt"), stderr, cancellationToken).ConfigureAwait(false);
    }

    private static void EnsureEvidenceOrSuccessfulExit(FileAdapterRequest request, int exitCode)
    {
        if (File.Exists(EvidencePath(request)))
        {
            return;
        }

        throw new FileAdapterProtocolException("LORQ-ADAPTER-EVIDENCE-MISSING", $"The external adapter exited with code {exitCode} without writing adapter-evidence.json.");
    }

    private static async ValueTask<FileAdapterEvidence> ReadEvidenceAsync(FileAdapterRequest request, CancellationToken cancellationToken)
    {
        var evidencePath = EvidencePath(request);
        var json = await File.ReadAllTextAsync(evidencePath, cancellationToken).ConfigureAwait(false);
        var evidence = JsonSerializer.Deserialize<FileAdapterEvidence>(json, FileAdapterJson.Options);
        if (evidence is null)
        {
            throw new FileAdapterProtocolException("LORQ-ADAPTER-EVIDENCE-INVALID", "The external adapter wrote an empty or invalid evidence contract.");
        }

        ValidateEvidence(request, evidence);
        return evidence;
    }

    private static void ValidateEvidence(FileAdapterRequest request, FileAdapterEvidence evidence)
    {
        Require(evidence.SchemaVersion == FileAdapterProtocol.EvidenceSchemaVersion, "LORQ-ADAPTER-EVIDENCE-SCHEMA", "The adapter evidence schema_version is not supported.");
        Require(evidence.ContractVersion == FileAdapterProtocol.ContractVersion, "LORQ-ADAPTER-EVIDENCE-CONTRACT", "The adapter evidence contract_version is not supported.");
        Require(evidence.CellId == request.Cell.CellId, "LORQ-ADAPTER-EVIDENCE-CELL", "The adapter evidence cell_id does not match the request cell_id.");
        Require(evidence.FinalAnswer is not null, "LORQ-ADAPTER-EVIDENCE-FINAL-ANSWER", "The adapter evidence must include final_answer.");
        Require(evidence.Usage is not null, "LORQ-ADAPTER-EVIDENCE-USAGE", "The adapter evidence must include usage.");
        Require(evidence.Process is not null, "LORQ-ADAPTER-EVIDENCE-PROCESS", "The adapter evidence must include process details.");
    }

    private static void Require(bool condition, string code, string message)
    {
        if (!condition)
        {
            throw new FileAdapterProtocolException(code, message);
        }
    }

    private static void KillProcessTree(System.Diagnostics.Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
        }
    }

    private static string RequestPath(FileAdapterRequest request)
    {
        return Path.Combine(request.Workspace.EvidenceDirectory, FileAdapterProtocol.RequestFileName);
    }

    private static string EvidencePath(FileAdapterRequest request)
    {
        return Path.Combine(request.Workspace.EvidenceDirectory, FileAdapterProtocol.EvidenceFileName);
    }
}
