namespace Lorq.Core;

public sealed record LorqPackageMergeRequest(
    IReadOnlyList<string> ShardRoots,
    string OutputRoot,
    string PackageId,
    string? BenchmarkPath = null,
    bool Strict = true);
