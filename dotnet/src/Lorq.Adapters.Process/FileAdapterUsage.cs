namespace Lorq.Adapters.Process;

/// <summary>
/// Token or cost usage observed by an adapter when available.
/// </summary>
public sealed record FileAdapterUsage(
    long InputTokens,
    long CachedInputTokens,
    long OutputTokens,
    long ReasoningOutputTokens,
    decimal EstimatedCostUsd);
