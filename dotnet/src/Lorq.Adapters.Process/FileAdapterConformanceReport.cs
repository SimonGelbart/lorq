namespace Lorq.Adapters.Process;

/// <summary>
/// Summary produced by the file adapter conformance probe.
/// </summary>
public sealed record FileAdapterConformanceReport(
    bool Ok,
    string ContractVersion,
    string RequestSchemaVersion,
    string EvidenceSchemaVersion,
    IReadOnlyList<FileAdapterConformanceScenarioResult> Scenarios)
{
    public int TotalCount => Scenarios.Count;

    public int PassedCount => Scenarios.Count(scenario => scenario.Passed);

    public int FailedCount => Scenarios.Count - PassedCount;
}
