namespace Lorq.Adapters.Process;

/// <summary>
/// Result for one file adapter conformance scenario.
/// </summary>
public sealed record FileAdapterConformanceScenarioResult(
    string Name,
    bool Passed,
    string? Code,
    string? FailureClass,
    string? Message,
    string ExchangeDirectory,
    string? AdapterId,
    IReadOnlyList<string> Observations);
