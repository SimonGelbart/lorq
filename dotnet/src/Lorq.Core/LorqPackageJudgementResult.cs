namespace Lorq.Core;

public sealed record LorqPackageJudgementResult(
    bool Ok,
    string PackageRoot,
    string JudgementName,
    string Backend,
    int CellCount,
    int JudgedCellCount,
    IReadOnlyList<string> MissingFixtureCellIds,
    IReadOnlyList<string> MissingExpectedCellIds,
    IReadOnlyDictionary<string, object?> ScoreSummary,
    IReadOnlyList<LorqDiagnostic> Diagnostics)
{
    public IReadOnlyList<LorqDiagnostic> Errors => Diagnostics.Where(diagnostic => diagnostic.Severity == "error").ToArray();
}
