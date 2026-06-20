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
        var packageId = new PackageId(YamlLite.RequiredString(manifest, "package_id", path));
        var packageKind = new PackageKind(YamlLite.RequiredString(manifest, "package_kind", path));
        var schemaVersion = new PackageSchemaVersion(YamlLite.RequiredInt(manifest, "package_schema_version", path));
        var declaredShards = YamlLite.OptionalStringList(manifest, "shards").Select(shardId => new ShardId(shardId)).ToArray();

        ValidateSchemaVersion(schemaVersion, path);
        ValidatePackageKind(packageKind, path);

        return new PackageManifest(packageId, packageKind, schemaVersion, declaredShards);
    }

    private void ValidateSchemaVersion(PackageSchemaVersion schemaVersion, string path)
    {
        if (!schemaVersion.IsSupported())
        {
            diagnostics.Error("LORQ020", $"Unsupported package_schema_version '{schemaVersion.Value}'.", path);
        }
    }

    private void ValidatePackageKind(PackageKind packageKind, string path)
    {
        if (!packageKind.IsSupported())
        {
            diagnostics.Error("LORQ021", $"Unsupported package_kind '{packageKind.Value}'.", path);
        }
    }
}
