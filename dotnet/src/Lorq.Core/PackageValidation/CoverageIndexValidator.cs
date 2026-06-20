using System.Text.Json;

namespace Lorq.Core.PackageValidation;

internal sealed class CoverageIndexValidator
{
    private readonly PackageValidationDiagnostics diagnostics;

    public CoverageIndexValidator(PackageValidationDiagnostics diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        this.diagnostics = diagnostics;
    }

    public PackageCoverageIndex Read(string path)
    {
        using var document = JsonHelpers.ReadDocument(path);
        var schema = JsonHelpers.RequiredString(document.RootElement, "schema_version", path);
        if (!schema.StartsWith("lorq.coverage.", StringComparison.Ordinal))
        {
            diagnostics.Error("LORQ030", $"Unexpected coverage schema_version '{schema}'.", path);
        }

        var present = JsonHelpers.OptionalStringArray(document.RootElement, "present_cell_ids");
        var expected = JsonHelpers.OptionalStringArray(document.RootElement, "expected_cell_ids");
        var missing = JsonHelpers.OptionalStringArray(document.RootElement, "missing_cells");
        ValidateCounts(document.RootElement, present, path);
        ValidatePresentCells(present, expected, path);
        ValidateMissingCells(present, expected, missing, path);

        return new PackageCoverageIndex(present, expected, missing);
    }

    public void ValidateCells(PackageCoverageIndex coverage, IReadOnlyList<RunCell> cells)
    {
        var indexedCellIds = cells.Select(cell => cell.CellId).Order(StringComparer.Ordinal).ToArray();
        var presentCellIds = coverage.PresentCellIds.Order(StringComparer.Ordinal).ToArray();
        if (!indexedCellIds.SequenceEqual(presentCellIds, StringComparer.Ordinal))
        {
            diagnostics.Error("LORQ090", ".lorq/cells index does not match coverage present_cell_ids.");
        }
    }

    private void ValidateCounts(JsonElement rootElement, IReadOnlyList<string> present, string path)
    {
        var declaredCellCount = JsonHelpers.OptionalInt(rootElement, "cell_count", -1);
        if (declaredCellCount != present.Count)
        {
            diagnostics.Error("LORQ031", $"coverage cell_count {declaredCellCount} does not match present_cell_ids count {present.Count}.", path);
        }
    }

    private void ValidatePresentCells(IReadOnlyList<string> present, IReadOnlyList<string> expected, string path)
    {
        foreach (var cellId in present.Except(expected, StringComparer.Ordinal))
        {
            diagnostics.Error("LORQ032", $"Present cell '{cellId}' is not listed in expected cells.", path);
        }
    }

    private void ValidateMissingCells(
        IReadOnlyList<string> present,
        IReadOnlyList<string> expected,
        IReadOnlyList<string> missing,
        string path)
    {
        var missingFromSet = expected.Except(present, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        if (!missing.SequenceEqual(missingFromSet, StringComparer.Ordinal))
        {
            diagnostics.Error("LORQ033", "coverage missing_cells does not match expected minus present cells.", path);
        }
    }
}
