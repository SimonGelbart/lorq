namespace Lorq.Adapters.Process;

/// <summary>
/// Immutable command line used to invoke one external file adapter process.
/// </summary>
public sealed record FileAdapterProcessCommand(
    string Executable,
    IReadOnlyList<string> Arguments,
    string? WorkingDirectory)
{
    public static FileAdapterProcessCommand Create(string executable, params string[] arguments)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executable);
        return new FileAdapterProcessCommand(executable, arguments, null);
    }

    public FileAdapterProcessCommand WithWorkingDirectory(string workingDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workingDirectory);
        return this with { WorkingDirectory = workingDirectory };
    }
}
