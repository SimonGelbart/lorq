using System.Text.Json;
using Lorq.Cli.Commands;

namespace Lorq.Cli;

public sealed class CliJsonResultWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private readonly TextWriter output;
    private readonly TextWriter error;

    public CliJsonResultWriter(TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        this.output = output;
        this.error = error;
    }

    public async ValueTask<int> WriteAsync(CommandResult result, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);
        await WritePayloadAsync(result, cancellationToken).ConfigureAwait(false);
        await WriteErrorAsync(result, cancellationToken).ConfigureAwait(false);
        return result.ExitCode;
    }

    private async ValueTask WritePayloadAsync(CommandResult result, CancellationToken cancellationToken)
    {
        if (!result.HasPayload)
        {
            return;
        }

        var json = JsonSerializer.Serialize(result.Payload, JsonOptions);
        await output.WriteLineAsync(json.AsMemory(), cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask WriteErrorAsync(CommandResult result, CancellationToken cancellationToken)
    {
        if (!result.HasError)
        {
            return;
        }

        await error.WriteLineAsync(result.ErrorMessage.AsMemory(), cancellationToken).ConfigureAwait(false);
    }
}
