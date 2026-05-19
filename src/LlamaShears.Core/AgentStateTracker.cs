using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Common;

namespace LlamaShears.Core;

public sealed class AgentStateTracker : IAgentStateTracker
{
    private readonly IDataContextScope _scope;

    public AgentStateTracker(IDataContextScope scope)
    {
        _scope = scope;
    }

    public void SetState(string channelId, string? eventId = null, Guid? correlationId = null, Guid? sessionId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channelId);
        _scope.SetItem(AgentState.DataKey, new AgentState(
            ChannelId: channelId,
            EventId: eventId ?? _scope.GetAgentConfig().Id,
            CorrelationId: correlationId ?? Guid.CreateVersion7())
        {
            SessionId = sessionId,
        });
    }
}
