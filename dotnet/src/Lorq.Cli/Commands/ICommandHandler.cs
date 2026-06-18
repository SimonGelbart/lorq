namespace Lorq.Cli.Commands;

/// <summary>
/// Handles one parsed CLI command.
/// </summary>
/// <typeparam name="TOptions">Parsed options accepted by the command.</typeparam>
public interface ICommandHandler<in TOptions>
    where TOptions : CommandOptions
{
    /// <summary>
    /// Executes the command.
    /// </summary>
    ValueTask<CommandResult> HandleAsync(TOptions options, CancellationToken cancellationToken = default);
}
