namespace Lorq.Cli.Commands;

public sealed record MergeShardsOptions(
    IReadOnlyList<string> ShardRoots,
    string OutputRoot,
    string PackageId,
    string? BenchmarkPath,
    bool Strict) : CommandOptions;
