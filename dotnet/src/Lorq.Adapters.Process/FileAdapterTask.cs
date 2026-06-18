namespace Lorq.Adapters.Process;

/// <summary>
/// Prompt material supplied to an adapter for one cell.
/// </summary>
public sealed record FileAdapterTask(string PromptPath, string PromptText);
