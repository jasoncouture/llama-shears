using LlamaShears.Core.Abstractions.Agent;

namespace LlamaShears.Core.Abstractions.Events.Agent;

/// <summary>
/// Carries both ends of a config diff so subscribers can decide between birth, tombstone,
/// and update cases.
/// </summary>
/// <param name="CurrentConfig">Last-known config before the change; <see langword="null"/> when this is a birth.</param>
/// <param name="UpdatedConfig">New config after the change; <see langword="null"/> when this is a tombstone.</param>
public sealed record ConfigurationChangedNotification(
    AgentConfig? CurrentConfig,
    AgentConfig? UpdatedConfig)
{
    /// <summary>Agent id, taken from whichever config is non-null.</summary>
    public string Name => UpdatedConfig?.Id ?? CurrentConfig?.Id ?? throw new InvalidOperationException("Both config instances are null, this is a bug");

    private bool IsValid => UpdatedConfig is not null || CurrentConfig is not null;

    /// <summary><see langword="true"/> when this notification represents an actual change (birth, tombstone, or hash-distinct update).</summary>
    public bool HasChanges => IsValid && (IsBirth || IsTombstone ||
                                          string.Equals(CurrentConfig!.Hash, UpdatedConfig!.Hash,
                                              StringComparison.OrdinalIgnoreCase));

    /// <summary><see langword="true"/> when a config has appeared for the first time.</summary>
    public bool IsBirth => IsValid && CurrentConfig is null && UpdatedConfig is not null;

    /// <summary><see langword="true"/> when a previously-known config has been removed.</summary>
    public bool IsTombstone => IsValid && CurrentConfig is not null && UpdatedConfig is null;

    /// <summary><see langword="true"/> when both old and new configs are present (i.e. a mutation).</summary>
    public bool IsUpdate => IsValid && CurrentConfig is not null && UpdatedConfig is not null;
}
