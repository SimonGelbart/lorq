namespace Lorq.Core.PackageValidation;

internal sealed record PackageCoverageIndex(
    IReadOnlyList<string> PresentCellIds,
    IReadOnlyList<string> ExpectedCellIds,
    IReadOnlyList<string> MissingCellIds);
