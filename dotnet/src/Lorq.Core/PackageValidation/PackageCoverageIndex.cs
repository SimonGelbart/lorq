namespace Lorq.Core.PackageValidation;

internal sealed record PackageCoverageIndex(
    IReadOnlyList<CellId> PresentCellIds,
    IReadOnlyList<CellId> ExpectedCellIds,
    IReadOnlyList<CellId> MissingCellIds);
