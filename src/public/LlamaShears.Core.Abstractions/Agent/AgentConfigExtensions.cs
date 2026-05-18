using LlamaShears.Core.Abstractions.Common;

namespace LlamaShears.Core.Abstractions.Agent;

/// <summary>
/// Convenience accessors for pulling the active <see cref="AgentConfig"/> off
/// an <see cref="IDataContextScope"/> without callers having to remember the
/// well-known key.
/// </summary>
public static class AgentConfigExtensions
{
    /// <param name="scope">Data-context scope to inspect.</param>
    extension(IDataContextScope? scope)
    {
        /// <summary>
        /// Returns the <see cref="AgentConfig"/> attached to the given scope under
        /// <see cref="AgentConfig.DataKey"/>, or <see langword="null"/> if none is set.
        /// </summary>
        /// <returns>The active agent configuration, or <see langword="null"/> when the scope has none.</returns>
        public AgentConfig? TryGetAgentConfig()
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
        public AgentConfig GetAgentConfig()
        {
            var config = scope.TryGetAgentConfig() ?? throw new InvalidOperationException(
                $"Tried to get current agent scope from {AgentConfig.DataKey}, but no config was found");
            return config;
        }
    }
}
