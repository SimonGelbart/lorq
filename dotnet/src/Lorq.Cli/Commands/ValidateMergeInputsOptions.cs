namespace Lorq.Cli.Commands;

public sealed record ValidateMergeInputsOptions(IReadOnlyList<string> ShardRoots) : CommandOptions;
