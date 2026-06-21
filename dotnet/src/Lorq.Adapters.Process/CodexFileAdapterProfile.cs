namespace Lorq.Adapters.Process;

/// <summary>
/// Built-in metadata for a Codex-oriented file adapter wrapper.
/// </summary>
public static class CodexFileAdapterProfile
{
    public const string Name = "codex-cli";
    public const string DefaultCodexCommand = "codex";
    public const string DefaultPermissionProfile = "local-smoke";
    public const string OutputFormat = "codex-jsonl";
    public static readonly IReadOnlyList<string> DefaultCodexArguments = new[] { "exec", "--json" };

    public static FileAdapterProcessCommand ApplyTo(
        FileAdapterProcessCommand command,
        string? codexCommand,
        IReadOnlyList<string> codexArguments)
    {
        ArgumentNullException.ThrowIfNull(command);
        var resolvedCodexCommand = string.IsNullOrWhiteSpace(codexCommand) ? DefaultCodexCommand : codexCommand;
        var resolvedCodexArguments = codexArguments.Count == 0 ? DefaultCodexArguments : codexArguments;
        return command
            .WithEnvironmentVariable("LORQ_ADAPTER_PROFILE", Name)
            .WithEnvironmentVariable("LORQ_CODEX_COMMAND", resolvedCodexCommand)
            .WithEnvironmentVariable("LORQ_CODEX_ARGUMENTS", string.Join(Environment.NewLine, resolvedCodexArguments))
            .WithEnvironmentVariable("LORQ_CODEX_OUTPUT_FORMAT", OutputFormat)
            .WithEnvironmentVariable("LORQ_CODEX_PERMISSION_PROFILE", DefaultPermissionProfile)
            .WithEnvironmentVariable("LORQ_CODEX_INVOCATION", "one-shot-file-adapter");
    }
}
