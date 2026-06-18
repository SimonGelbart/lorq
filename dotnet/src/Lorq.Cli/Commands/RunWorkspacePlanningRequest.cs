namespace Lorq.Cli.Commands;

internal sealed record RunWorkspacePlanningRequest(
    string SuiteRoot,
    string OutputRoot,
    string ShardId,
    DeterministicBenchmarkCell Cell,
    string? WorkRoot);
