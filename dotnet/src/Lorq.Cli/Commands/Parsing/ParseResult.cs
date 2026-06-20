namespace Lorq.Cli.Commands.Parsing;

/// <summary>
/// Result of parsing command-line arguments into a typed options object.
/// </summary>
public sealed record ParseResult<TOptions>(TOptions? Options, string? ErrorMessage)
    where TOptions : CommandOptions
{
    public bool Ok => Options is not null;

    public static ParseResult<TOptions> Success(TOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return new ParseResult<TOptions>(options, null);
    }

    public static ParseResult<TOptions> Failure(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        return new ParseResult<TOptions>(null, message);
    }
}
