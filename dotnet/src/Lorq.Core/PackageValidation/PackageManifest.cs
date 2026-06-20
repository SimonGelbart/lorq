namespace Lorq.Core.PackageValidation;

internal sealed record PackageManifest(
    PackageId PackageId,
    PackageKind PackageKind,
    PackageSchemaVersion SchemaVersion,
    IReadOnlyList<ShardId> DeclaredShards);
