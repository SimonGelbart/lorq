using Lorq.Adapters.Process;
using Lorq.Cli.Commands.Parsing;

namespace Lorq.Cli.Runtime;

internal sealed class RunAdapterFactory
{
    public IFileAdapter Create(RunOptions options, string suiteRoot)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(suiteRoot);
        if (!string.IsNullOrWhiteSpace(options.AdapterCommand))
        {
            return CreateExternalAdapter(options, suiteRoot);
        }

        var fixture = DeterministicFakeAgentFixture.Load(ResolveFromSuite(suiteRoot, options.AdapterFixturePath));
        return new DeterministicFakeFileAdapter(fixture);
    }

    private static IFileAdapter CreateExternalAdapter(RunOptions options, string suiteRoot)
    {
        var workingDirectory = ResolveOptionalWorkingDirectory(options.AdapterWorkingDirectory, suiteRoot);
        var command = new FileAdapterProcessCommand(options.AdapterCommand!, options.AdapterArguments, workingDirectory, new Dictionary<string, string>());
        return new ExternalFileAdapterProcess(ApplyAdapterProfile(options, command));
    }

    private static FileAdapterProcessCommand ApplyAdapterProfile(RunOptions options, FileAdapterProcessCommand command)
    {
        if (string.IsNullOrWhiteSpace(options.AdapterProfile))
        {
            return command;
        }

        if (string.Equals(options.AdapterProfile, CodexFileAdapterProfile.Name, StringComparison.Ordinal))
        {
            return CodexFileAdapterProfile.ApplyTo(command, options.CodexCommand, options.CodexArguments);
        }

        if (string.Equals(options.AdapterProfile, CopilotSdkFileAdapterProfile.Name, StringComparison.Ordinal))
        {
            return CopilotSdkFileAdapterProfile.ApplyTo(command);
        }

        throw new FileAdapterProtocolException("LORQ-ADAPTER-PROFILE", $"Unknown adapter profile '{options.AdapterProfile}'.");
    }

    private static string? ResolveOptionalWorkingDirectory(string? workingDirectory, string suiteRoot)
    {
        if (string.IsNullOrWhiteSpace(workingDirectory))
        {
            return null;
        }

        return Path.IsPathRooted(workingDirectory) ? workingDirectory : Path.GetFullPath(Path.Combine(suiteRoot, workingDirectory));
    }

    private static string ResolveFromSuite(string suiteRoot, string path)
    {
        return Path.IsPathRooted(path) ? path : Path.Combine(suiteRoot, path);
    }
}
