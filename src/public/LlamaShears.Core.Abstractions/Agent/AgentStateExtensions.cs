using LlamaShears.Core.Abstractions.Common;

namespace LlamaShears.Core.Abstractions.Agent;

/// <summary>
/// Convenience accessors for pulling the active <see cref="AgentState"/> off
/// an <see cref="IDataContextScope"/> without callers having to remember the
/// well-known key, plus per-field shortcuts that delegate to the same lookup.
/// </summary>
public static class AgentStateExtensions
{
    extension(IDataContextScope? scope)
    {
        /// <summary>
        /// Returns the <see cref="AgentState"/> attached to the given scope under
        /// <see cref="AgentState.DataKey"/>, or <see langword="null"/> if none is set.
        /// </summary>
        public AgentState? TryGetAgentState()
        {
            if (scope is null) return null;
            scope.TryGetValue<AgentState>(AgentState.DataKey, out var state);
            return state;
        }

        /// <summary>
        /// Returns the <see cref="AgentState"/> attached to the given scope under
        /// <see cref="AgentState.DataKey"/>. Throws when the scope is
        /// <see langword="null"/> or has no state stashed.
        /// </summary>
        public AgentState GetAgentState()
        {
            var state = scope.TryGetAgentState() ?? throw new InvalidOperationException(
                $"Tried to get current agent state from {AgentState.DataKey}, but no state was found");
            return state;
        }

        /// <summary>Returns the active channel id, or <see langword="null"/> when no agent state is set.</summary>
        public string? TryGetChannelId() => scope.TryGetAgentState()?.ChannelId;

        /// <summary>Returns the active channel id. Throws when no agent state is set.</summary>
        public string GetChannelId() => scope.GetAgentState().ChannelId;

        /// <summary>Returns the active event id, or <see langword="null"/> when no agent state is set.</summary>
        public string? TryGetEventId() => scope.TryGetAgentState()?.EventId;

        /// <summary>Returns the active event id. Throws when no agent state is set.</summary>
        public string GetEventId() => scope.GetAgentState().EventId;

        /// <summary>Returns the active correlation id, or <see langword="null"/> when no agent state is set.</summary>
        public Guid? TryGetCorrelationId() => scope.TryGetAgentState()?.CorrelationId;

        /// <summary>Returns the active correlation id. Throws when no agent state is set.</summary>
        public Guid GetCorrelationId() => scope.GetAgentState().CorrelationId;
    }
}
