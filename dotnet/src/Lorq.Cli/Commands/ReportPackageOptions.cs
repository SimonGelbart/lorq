namespace Lorq.Cli.Commands;

public sealed record ReportPackageOptions(string PackageRoot, string PrimaryJudgement) : CommandOptions;
