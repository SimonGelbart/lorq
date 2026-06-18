namespace Lorq.Cli.Commands;

/// <summary>
/// Exit code plus optional payload/error produced by a CLI command.
/// </summary>
public sealed record CommandResult(int ExitCode, object? Payload, string? ErrorMessage)
{
    public bool HasPayload => Payload is not null;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public static CommandResult Success(object payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return new CommandResult(0, payload, null);
    }

    public static CommandResult Failure(object payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return new CommandResult(1, payload, null);
    }

    public static CommandResult UsageError(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        return new CommandResult(2, null, message);
    }
}
