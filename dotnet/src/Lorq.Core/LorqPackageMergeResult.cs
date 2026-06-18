namespace Lorq.Core;

public sealed record LorqPackageMergeResult(
    bool Ok,
    string PackageRoot,
    string PackageId,
    IReadOnlyList<string> ShardIds,
    int CellCount,
    int ExpectedCellCount,
    IReadOnlyList<string> MissingCellIds,
    IReadOnlyList<string> DuplicateCellIds,
    bool FingerprintMismatch,
    IReadOnlyList<LorqDiagnostic> Diagnostics)
{
    public IReadOnlyList<LorqDiagnostic> Errors => Diagnostics.Where(diagnostic => diagnostic.Severity == "error").ToArray();
}
