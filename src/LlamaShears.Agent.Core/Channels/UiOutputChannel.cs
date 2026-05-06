using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Events;
using LlamaShears.Core.Abstractions.Provider;
using MessagePipe;

namespace LlamaShears.Agent.Core.Channels;

public sealed class UiOutputChannel : IOutputChannel
{
    private readonly string _agentId;
    private readonly IAsyncPublisher<AgentTurnEmitted> _publisher;

    public UiOutputChannel(string agentId, IAsyncPublisher<AgentTurnEmitted> publisher)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);
        ArgumentNullException.ThrowIfNull(publisher);
        _agentId = agentId;
        _publisher = publisher;
    }

    public async Task SendAsync(ModelTurn turn, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(turn);
        await _publisher.PublishAsync(new AgentTurnEmitted(_agentId, turn), cancellationToken).ConfigureAwait(false);
    }
}
