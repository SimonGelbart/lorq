namespace Lorq.Adapters.Process;

/// <summary>
/// Maps low-level adapter diagnostics to product-facing failure classes.
/// </summary>
public static class FileAdapterFailureClassifier
{
    public const string AdapterFailed = "adapter_failed";
    public const string InvalidArtifact = "invalid_artifact";
    public const string NoFinalAnswer = "no_final_answer";
    public const string PermissionDenied = "permission_denied";
    public const string SetupFailure = "setup_failure";
    public const string Timeout = "timeout";

    public static string ClassifyDiagnosticCode(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        return code switch
        {
            "LORQ-ADAPTER-PROCESS-TIMEOUT" => Timeout,
            "LORQ-ADAPTER-PROCESS-START" => SetupFailure,
            "LORQ-ADAPTER-EVIDENCE-MISSING" => AdapterFailed,
            "LORQ-ADAPTER-EVIDENCE-FINAL-ANSWER" => NoFinalAnswer,
            "LORQ-ADAPTER-CONFORMANCE-FILES" => InvalidArtifact,
            _ => SetupFailure,
        };
    }

    public static string ClassifyEvidenceStatus(string status)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(status);
        return status switch
        {
            AdapterFailed => AdapterFailed,
            InvalidArtifact => InvalidArtifact,
            NoFinalAnswer => NoFinalAnswer,
            PermissionDenied => PermissionDenied,
            Timeout => Timeout,
            _ => SetupFailure,
        };
    }
}
