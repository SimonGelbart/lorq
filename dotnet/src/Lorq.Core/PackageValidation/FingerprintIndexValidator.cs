using System.Text.Json;

namespace Lorq.Core.PackageValidation;

internal sealed class FingerprintIndexValidator
{
    private readonly PackageValidationDiagnostics diagnostics;

    public FingerprintIndexValidator(PackageValidationDiagnostics diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        this.diagnostics = diagnostics;
    }

    public IReadOnlyDictionary<string, string> Read(string path)
    {
        using var document = JsonHelpers.ReadDocument(path);
        var schema = JsonHelpers.RequiredString(document.RootElement, "schema_version", path);
        if (!schema.StartsWith("lorq.fingerprints.", StringComparison.Ordinal))
        {
            diagnostics.Error("LORQ040", $"Unexpected fingerprint schema_version '{schema}'.", path);
        }

        if (!document.RootElement.TryGetProperty("by_cell", out var byCell) || byCell.ValueKind != JsonValueKind.Object)
        {
            diagnostics.Error("LORQ041", "fingerprints.json must contain object property by_cell.", path);
            return new Dictionary<string, string>();
        }

        return byCell.EnumerateObject().ToDictionary(
            property => property.Name,
            property => JsonCanonicalizer.Canonicalize(property.Value),
            StringComparer.Ordinal);
    }

    public void ValidateCells(IReadOnlyDictionary<string, string> fingerprints, IReadOnlyList<RunCell> cells)
    {
        var missing = cells.Select(cell => cell.CellId).Except(fingerprints.Keys, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        foreach (var cellId in missing)
        {
            diagnostics.Error("LORQ100", $"Cell '{cellId}' is missing from fingerprints by_cell.");
        }
    }
}
