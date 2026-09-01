using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Channel;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Core;

public sealed class ChannelMessageIntakeService : IAgentService, IEventHandler<ChannelMessage>
{
    private readonly IEventBus _bus;
    private readonly ISessionQueue _sessionQueue;
    private readonly IDataContextScope _dataScope;
    private IDisposable? _subscription;

    public ChannelMessageIntakeService(
        IEventBus bus,
        ISessionQueue sessionQueue,
        IDataContextScope dataScope)
    {
        _bus = bus;
        _sessionQueue = sessionQueue;
        _dataScope = dataScope;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _subscription = _bus.Subscribe<ChannelMessage>(
            Event.WellKnown.Channel.Message with { Id = _dataScope.GetCurrentSessionId() },
            EventDeliveryMode.Awaited,
            this,
            preserveSubscriberExecutionContext: true);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _subscription?.Dispose();
        _subscription = null;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async ValueTask HandleAsync(IEventEnvelope<ChannelMessage> envelope, CancellationToken cancellationToken)
    {
        var data = envelope.Data;
        if (data is null)
        {
            return;
        }

        var turn = new ModelTurn(
            ModelRole.User,
            data.Text,
            data.Timestamp,
            ChannelId: data.ChannelId)
        {
            Attachments = data.Attachments,
        };
        await _sessionQueue.EnqueueAsync(turn, cancellationToken);
    }
}
