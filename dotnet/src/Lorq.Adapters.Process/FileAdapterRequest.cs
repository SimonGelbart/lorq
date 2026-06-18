namespace Lorq.Adapters.Process;

/// <summary>
/// Adapter input contract written by LORQ before launching a one-shot adapter.
/// </summary>
public sealed record FileAdapterRequest(
    string SchemaVersion,
    string ContractVersion,
    FileAdapterCell Cell,
    FileAdapterWorkspace Workspace,
    FileAdapterTask Task,
    FileAdapterLimits Limits,
    FileAdapterExpectedOutput ExpectedOutput);
