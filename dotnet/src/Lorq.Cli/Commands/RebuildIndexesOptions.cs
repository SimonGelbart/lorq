namespace Lorq.Cli.Commands;

public sealed record RebuildIndexesOptions(string PackageRoot, string TargetRoot) : CommandOptions;
