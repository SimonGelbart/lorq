namespace Lorq.Core.PackageValidation;

internal sealed record PackageRequiredFileSet(
    string ExperimentYaml,
    string CoveragePath,
    string FingerprintsPath,
    string IntegrityPath,
    string MergeLogPath);
