namespace Lorq.Adapters.Process;

/// <summary>
/// Built-in metadata for a Copilot SDK file-adapter smoke wrapper.
/// </summary>
public static class CopilotSdkFileAdapterProfile
{
    public const string Name = "copilot-sdk";
    public const string OutputFormat = "copilot-sdk-transcript";
    public const string DefaultPermissionProfile = "local-smoke";

    public static FileAdapterProcessCommand ApplyTo(FileAdapterProcessCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        return command
            .WithEnvironmentVariable("LORQ_ADAPTER_PROFILE", Name)
            .WithEnvironmentVariable("LORQ_COPILOT_OUTPUT_FORMAT", OutputFormat)
            .WithEnvironmentVariable("LORQ_COPILOT_PERMISSION_PROFILE", DefaultPermissionProfile)
            .WithEnvironmentVariable("LORQ_COPILOT_INVOCATION", "one-shot-file-adapter");
    }

    public static FileAdapterRuntimeMetadata RuntimeMetadata(string? command = null)
    {
        return new FileAdapterRuntimeMetadata(
            "github",
            "copilot-sdk",
            null,
            Name,
            command,
            DefaultPermissionProfile,
            OutputFormat,
            new Dictionary<string, string>());
    }
}
