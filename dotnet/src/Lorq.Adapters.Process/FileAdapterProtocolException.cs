namespace Lorq.Adapters.Process;

/// <summary>
/// Raised when an external adapter violates the file-based one-shot protocol.
/// </summary>
public sealed class FileAdapterProtocolException : Exception
{
    public FileAdapterProtocolException(string code, string message)
        : base(message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        Code = code;
    }

    public string Code { get; }
}
