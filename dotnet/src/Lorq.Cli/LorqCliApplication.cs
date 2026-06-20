using Lorq.Cli.Commands;
using Microsoft.Extensions.DependencyInjection;
using Lorq.Cli.Commands.Results;
using Lorq.Cli.Hosting;

namespace Lorq.Cli;

public sealed class LorqCliApplication
{
    private readonly LorqCommandCatalog catalog;

    public LorqCliApplication(LorqCommandCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        this.catalog = catalog;
    }

    public static async ValueTask<int> RunAsync(string[] args, TextWriter output, TextWriter error, CancellationToken cancellationToken = default)
    {
        using var host = LorqCliHost.Build(args);
        var application = host.Services.GetRequiredService<LorqCliApplication>();
        return await application.ExecuteAsync(args, output, error, cancellationToken).ConfigureAwait(false);
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
