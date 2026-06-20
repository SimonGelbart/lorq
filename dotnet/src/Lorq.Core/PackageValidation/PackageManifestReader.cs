namespace Lorq.Core.PackageValidation;

internal sealed class PackageManifestReader
{
    private readonly PackageValidationDiagnostics diagnostics;

    public PackageManifestReader(PackageValidationDiagnostics diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        this.diagnostics = diagnostics;
    }

    public PackageManifest Read(string path)
    {
        var manifest = YamlLite.ParseTopLevel(path);
        var packageId = YamlLite.RequiredString(manifest, "package_id", path);
        var packageKind = YamlLite.RequiredString(manifest, "package_kind", path);
        var schemaVersion = YamlLite.RequiredInt(manifest, "package_schema_version", path);
        var declaredShards = YamlLite.OptionalStringList(manifest, "shards");

        ValidateSchemaVersion(schemaVersion, path);
        ValidatePackageKind(packageKind, path);

        return new PackageManifest(packageId, packageKind, schemaVersion, declaredShards);
    }

    private void ValidateSchemaVersion(int schemaVersion, string path)
    {
        if (schemaVersion != 1)
        {
            diagnostics.Error("LORQ020", $"Unsupported package_schema_version '{schemaVersion}'.", path);
        }
    }

    private void ValidatePackageKind(string packageKind, string path)
    {
        if (packageKind is not ("run_shard" or "merged_experiment"))
        {
            diagnostics.Error("LORQ021", $"Unsupported package_kind '{packageKind}'.", path);
        }
    }
}
