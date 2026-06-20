namespace Lorq.Core.PackageValidation;

internal sealed class MergeLogValidator
{
    private readonly PackageValidationDiagnostics diagnostics;

    public MergeLogValidator(PackageValidationDiagnostics diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        this.diagnostics = diagnostics;
    }

    public void Validate(string path, string packageKind)
    {
        using var document = JsonHelpers.ReadDocument(path);
        var schema = JsonHelpers.RequiredString(document.RootElement, "schema_version", path);
        if (!schema.StartsWith("lorq.merge-log.", StringComparison.Ordinal))
        {
            diagnostics.Error("LORQ060", $"Unexpected merge-log schema_version '{schema}'.", path);
        }

        var operation = JsonHelpers.RequiredString(document.RootElement, "operation", path);
        if (packageKind == "merged_experiment" && operation != "python-v0-merge-run-shards")
        {
            diagnostics.Error("LORQ061", $"Merged package has unexpected merge operation '{operation}'.", path);
        }
    }
}
