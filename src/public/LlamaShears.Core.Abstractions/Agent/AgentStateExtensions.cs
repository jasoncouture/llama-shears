using LlamaShears.Core.Abstractions.Common;

namespace LlamaShears.Core.Abstractions.Agent;

/// <summary>
/// Convenience accessors for pulling the active <see cref="AgentState"/> off
/// an <see cref="IDataContextScope"/> without callers having to remember the
/// well-known key, plus per-field shortcuts that delegate to the same lookup.
/// </summary>
public static class AgentStateExtensions
{
    /// <summary>
    /// Returns the <see cref="AgentState"/> attached to the given scope under
    /// <see cref="AgentState.DataKey"/>, or <see langword="null"/> if none is set.
    /// </summary>
    public static AgentState? TryGetAgentState(this IDataContextScope? scope)
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
    public static AgentState GetAgentState(this IDataContextScope? scope)
    {
        var state = scope.TryGetAgentState() ?? throw new InvalidOperationException(
            $"Tried to get current agent state from {AgentState.DataKey}, but no state was found");
        return state;
    }

    /// <summary>Returns the active channel id, or <see langword="null"/> when no agent state is set.</summary>
    public static string? TryGetChannelId(this IDataContextScope? scope) => scope.TryGetAgentState()?.ChannelId;

    /// <summary>Returns the active channel id. Throws when no agent state is set.</summary>
    public static string GetChannelId(this IDataContextScope? scope) => scope.GetAgentState().ChannelId;

    /// <summary>Returns the active event id, or <see langword="null"/> when no agent state is set.</summary>
    public static string? TryGetEventId(this IDataContextScope? scope) => scope.TryGetAgentState()?.EventId;

    /// <summary>Returns the active event id. Throws when no agent state is set.</summary>
    public static string GetEventId(this IDataContextScope? scope) => scope.GetAgentState().EventId;

    /// <summary>Returns the active correlation id, or <see langword="null"/> when no agent state is set.</summary>
    public static Guid? TryGetCorrelationId(this IDataContextScope? scope) => scope.TryGetAgentState()?.CorrelationId;

    /// <summary>Returns the active correlation id. Throws when no agent state is set.</summary>
    public static Guid GetCorrelationId(this IDataContextScope? scope) => scope.GetAgentState().CorrelationId;
}
