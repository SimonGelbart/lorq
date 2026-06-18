namespace Lorq.Adapters.Process;

/// <summary>
/// Immutable command line used to invoke one external file adapter process.
/// </summary>
public sealed record FileAdapterProcessCommand(
    string Executable,
    IReadOnlyList<string> Arguments,
    string? WorkingDirectory,
    IReadOnlyDictionary<string, string> EnvironmentVariables)
{
    public static FileAdapterProcessCommand Create(string executable, params string[] arguments)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executable);
        return new FileAdapterProcessCommand(executable, arguments, null, new Dictionary<string, string>());
    }

    public FileAdapterProcessCommand WithWorkingDirectory(string workingDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workingDirectory);
        return this with { WorkingDirectory = workingDirectory };
    }

    public FileAdapterProcessCommand WithEnvironmentVariable(string name, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(value);
        var next = new Dictionary<string, string>(EnvironmentVariables, StringComparer.Ordinal)
        {
            [name] = value,
        };
        return this with { EnvironmentVariables = next };
    }
}
