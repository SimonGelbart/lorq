using System.Text.Json;

namespace Lorq.Core.PackageValidation;

internal sealed class IntegrityIndexReader
{
    private readonly PackageValidationDiagnostics diagnostics;

    public IntegrityIndexReader(PackageValidationDiagnostics diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        this.diagnostics = diagnostics;
    }

    public PackageIntegrityIndex Read(string path)
    {
        using var document = JsonHelpers.ReadDocument(path);
        var schema = JsonHelpers.RequiredString(document.RootElement, "schema_version", path);
        if (!schema.StartsWith("lorq.integrity.", StringComparison.Ordinal))
        {
            diagnostics.Error("LORQ050", $"Unexpected integrity schema_version '{schema}'.", path);
        }

        var ok = JsonHelpers.OptionalBool(document.RootElement, "ok", false);
        var warningCount = document.RootElement.TryGetProperty("warnings", out var warnings) && warnings.ValueKind == JsonValueKind.Array
            ? warnings.GetArrayLength()
            : 0;
        return new PackageIntegrityIndex(ok, warningCount);
    }
}
