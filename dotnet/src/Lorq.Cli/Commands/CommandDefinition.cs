using Lorq.Cli.Commands.Parsing;
using Lorq.Cli.Commands.Results;

namespace Lorq.Cli.Commands;

public sealed class CommandDefinition<TOptions> : ICommandDefinition
    where TOptions : CommandOptions
{
    private readonly Func<IReadOnlyList<string>, ParseResult<TOptions>> parse;
    private readonly ICommandHandler<TOptions> handler;

    public CommandDefinition(string name, Func<IReadOnlyList<string>, ParseResult<TOptions>> parse, ICommandHandler<TOptions> handler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(parse);
        ArgumentNullException.ThrowIfNull(handler);
        Name = name;
        this.parse = parse;
        this.handler = handler;
    }

    public string Name { get; }

    public ValueTask<CommandResult> ExecuteAsync(IReadOnlyList<string> values, CancellationToken cancellationToken = default)
    {
        var result = parse(values);
        return result.Options is null
            ? ValueTask.FromResult(CommandResult.UsageError(result.ErrorMessage ?? CommandUsage.Text))
            : handler.HandleAsync(result.Options, cancellationToken);
    }
}
