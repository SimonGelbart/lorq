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

        var present = ReadCellIds(document.RootElement, "present_cell_ids");
        var expected = ReadCellIds(document.RootElement, "expected_cell_ids");
        var missing = ReadCellIds(document.RootElement, "missing_cells");
        ValidateCounts(document.RootElement, present, path);
        ValidatePresentCells(present, expected, path);
        ValidateMissingCells(present, expected, missing, path);

        return new PackageCoverageIndex(present, expected, missing);
    }

    public void ValidateCells(PackageCoverageIndex coverage, IReadOnlyList<RunCell> cells)
    {
        var indexedCellIds = cells.Select(cell => new CellId(cell.CellId)).OrderBy(cellId => cellId.Value, StringComparer.Ordinal).ToArray();
        var presentCellIds = coverage.PresentCellIds.OrderBy(cellId => cellId.Value, StringComparer.Ordinal).ToArray();
        if (!indexedCellIds.SequenceEqual(presentCellIds))
        {
            diagnostics.Error("LORQ090", ".lorq/cells index does not match coverage present_cell_ids.");
        }
    }

    private static IReadOnlyList<CellId> ReadCellIds(JsonElement rootElement, string propertyName)
    {
        return JsonHelpers.OptionalStringArray(rootElement, propertyName).Select(cellId => new CellId(cellId)).ToArray();
    }

    private void ValidateCounts(JsonElement rootElement, IReadOnlyList<CellId> present, string path)
    {
        var declaredCellCount = JsonHelpers.OptionalInt(rootElement, "cell_count", -1);
        if (declaredCellCount != present.Count)
        {
            diagnostics.Error("LORQ031", $"coverage cell_count {declaredCellCount} does not match present_cell_ids count {present.Count}.", path);
        }
    }

    private void ValidatePresentCells(IReadOnlyList<CellId> present, IReadOnlyList<CellId> expected, string path)
    {
        foreach (var cellId in present.Except(expected).OrderBy(cellId => cellId.Value, StringComparer.Ordinal))
        {
            diagnostics.Error("LORQ032", $"Present cell '{cellId.Value}' is not listed in expected cells.", path);
        }
    }

    private void ValidateMissingCells(
        IReadOnlyList<CellId> present,
        IReadOnlyList<CellId> expected,
        IReadOnlyList<CellId> missing,
        string path)
    {
        var missingFromSet = expected.Except(present).OrderBy(cellId => cellId.Value, StringComparer.Ordinal).ToArray();
        if (!missing.SequenceEqual(missingFromSet))
        {
            diagnostics.Error("LORQ033", "coverage missing_cells does not match expected minus present cells.", path);
        }
    }
}
