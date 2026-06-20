namespace Lorq.Core.PackageValidation;

internal sealed record PackageManifest(
    string PackageId,
    string PackageKind,
    int SchemaVersion,
    IReadOnlyList<string> DeclaredShards);
