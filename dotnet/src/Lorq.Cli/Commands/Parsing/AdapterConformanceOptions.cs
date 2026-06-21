namespace Lorq.Cli.Commands.Parsing;

public sealed record AdapterConformanceOptions(
    string AdapterCommand,
    IReadOnlyList<string> AdapterArguments,
    string? AdapterWorkingDirectory,
    string OutputRoot,
    int TimeoutMilliseconds) : CommandOptions;
