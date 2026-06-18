namespace Lorq.Cli.Commands;

public sealed record JudgePackageOptions(
    string PackageRoot,
    string JudgeName,
    string FixturePath,
    bool Strict) : CommandOptions;
