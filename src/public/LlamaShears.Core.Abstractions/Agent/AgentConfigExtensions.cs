using LlamaShears.Core.Abstractions.Common;

namespace LlamaShears.Core.Abstractions.Agent;

/// <summary>
/// Convenience accessors for pulling the active <see cref="AgentConfig"/> off
/// an <see cref="IDataContextScope"/> without callers having to remember the
/// well-known key.
/// </summary>
public static class AgentConfigExtensions
{
    /// <summary>
    /// Returns the <see cref="AgentConfig"/> attached to the given scope under
    /// <see cref="AgentConfig.DataKey"/>, or <see langword="null"/> if none is set.
    /// </summary>
    /// <param name="scope">Data-context scope to inspect.</param>
    /// <returns>The active agent configuration, or <see langword="null"/> when the scope has none.</returns>
    public static AgentConfig? TryGetAgentConfig(this IDataContextScope? scope)
    {
        if (scope is null) return null;
        scope.TryGetValue<AgentConfig>(AgentConfig.DataKey, out var config);
        return config;
    }

    /// <summary>
    /// Returns the <see cref="AgentConfig"/> attached to the given scope under
    /// <see cref="AgentConfig.DataKey"/>. Throws when the scope is
    /// <see langword="null"/> or has no config stashed; intended for sites
    /// that legitimately cannot proceed without one.
    /// </summary>
    public static AgentConfig GetAgentConfig(this IDataContextScope? scope)
    {
        var config = scope.TryGetAgentConfig() ?? throw new InvalidOperationException(
                $"Tried to get current agent scope from {AgentConfig.DataKey}, but no config was found");
        return config;
    }
}
