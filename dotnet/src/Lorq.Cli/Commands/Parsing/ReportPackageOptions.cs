namespace Lorq.Cli.Commands.Parsing;

public sealed record ReportPackageOptions(string PackageRoot, string PrimaryJudgement) : CommandOptions;
