namespace Lorq.Cli.Commands;

public sealed class LorqCommandCatalog
{
    private readonly IReadOnlyDictionary<string, ICommandDefinition> commands;

    public LorqCommandCatalog(IEnumerable<ICommandDefinition> commands)
    {
        ArgumentNullException.ThrowIfNull(commands);
        this.commands = commands.ToDictionary(command => command.Name, StringComparer.Ordinal);
    }

    public bool TryFind(string name, out ICommandDefinition command)
    {
        return commands.TryGetValue(name, out command!);
    }
}
