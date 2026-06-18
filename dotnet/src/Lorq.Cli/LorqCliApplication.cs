using Lorq.Cli.Commands;

namespace Lorq.Cli;

public sealed class LorqCliApplication
{
    private readonly LorqCommandCatalog catalog;

    public LorqCliApplication(LorqCommandCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        this.catalog = catalog;
    }

    public static ValueTask<int> RunAsync(string[] args, TextWriter output, TextWriter error, CancellationToken cancellationToken = default)
    {
        var application = new LorqCliApplication(LorqCommandCatalog.CreateDefault());
        return application.ExecuteAsync(args, output, error, cancellationToken);
    }

    public async ValueTask<int> ExecuteAsync(IReadOnlyList<string> args, TextWriter output, TextWriter error, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        var result = await ExecuteCommandAsync(args, cancellationToken).ConfigureAwait(false);
        var writer = new CliJsonResultWriter(output, error);
        return await writer.WriteAsync(result, cancellationToken).ConfigureAwait(false);
    }

    private ValueTask<CommandResult> ExecuteCommandAsync(IReadOnlyList<string> args, CancellationToken cancellationToken)
    {
        if (args.Count < 2)
        {
            return ValueTask.FromResult(CommandResult.UsageError(CommandUsage.Text));
        }

        if (!catalog.TryFind(args[0], out var command))
        {
            return ValueTask.FromResult(CommandResult.UsageError($"Unknown command '{args[0]}'."));
        }

        return command.ExecuteAsync(args.Skip(1).ToArray(), cancellationToken);
    }
}
