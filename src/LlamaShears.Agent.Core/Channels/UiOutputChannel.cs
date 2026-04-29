using LlamaShears.Agent.Abstractions;
using LlamaShears.Agent.Abstractions.Events;
using LlamaShears.Provider.Abstractions;
using MessagePipe;

namespace LlamaShears.Agent.Core.Channels;

/// <summary>
/// Output channel that re-publishes each completed
/// <see cref="ModelTurn"/> as an <see cref="AgentTurnEmitted"/> event on
/// the in-process bus. UI subscribers consume the event to render or
/// persist the turn.
/// </summary>
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
