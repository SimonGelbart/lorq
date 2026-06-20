namespace Lorq.Cli.Commands.Parsing;

public sealed record RebuildIndexesOptions(string PackageRoot, string TargetRoot) : CommandOptions;
