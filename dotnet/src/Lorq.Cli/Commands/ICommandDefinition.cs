using Lorq.Cli.Commands.Parsing;
using Lorq.Cli.Commands.Results;

namespace Lorq.Cli.Commands;

public interface ICommandDefinition
{
    string Name { get; }

    ValueTask<CommandResult> ExecuteAsync(IReadOnlyList<string> values, CancellationToken cancellationToken = default);
}
