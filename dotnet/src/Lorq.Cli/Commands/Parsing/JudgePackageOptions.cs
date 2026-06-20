namespace Lorq.Cli.Commands.Parsing;

public sealed record JudgePackageOptions(
    string PackageRoot,
    string JudgeName,
    string FixturePath,
    bool Strict) : CommandOptions;
