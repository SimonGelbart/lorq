namespace Lorq.Cli.Commands;

public sealed record RunOptions(
    string OutputRoot,
    string SuiteRoot,
    string ShardId,
    string PackageId,
    string BenchmarkPath,
    string AdapterFixturePath,
    bool NoJudge,
    string? AdapterCommand,
    IReadOnlyList<string> AdapterArguments,
    string? AdapterWorkingDirectory,
    string? AdapterProfile,
    string? CodexCommand,
    IReadOnlyList<string> CodexArguments) : CommandOptions;
