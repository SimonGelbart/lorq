namespace Lorq.Cli.Runtime;

internal sealed record RunWorkspacePlanningRequest(
    string SuiteRoot,
    string OutputRoot,
    string ShardId,
    DeterministicBenchmarkCell Cell,
    string? WorkRoot);
