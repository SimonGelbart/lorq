namespace Lorq.Cli.Commands;

public interface ICommandDefinition
{
    string Name { get; }

    ValueTask<CommandResult> ExecuteAsync(IReadOnlyList<string> values, CancellationToken cancellationToken = default);
}
