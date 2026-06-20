namespace Lorq.Cli.Commands.Parsing;

public sealed record ValidateMergeInputsOptions(IReadOnlyList<string> ShardRoots) : CommandOptions;
