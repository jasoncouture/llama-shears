using LlamaShears.Core.Abstractions;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Agent;
using LlamaShears.Core.Abstractions.Events.Channel;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Core;

public sealed class TransientAgent : ITransientAgent, IEventHandler<AgentLifecycleEvent>, IEventHandler<ModelTurn>
{
    private readonly IAgent _agent;
    private readonly IEventBus _eventBus;
    private readonly ISessionQueue _sessionQueue;
    private readonly EventType _messageEvent;
    private readonly EventType _stopEvent;
    private readonly EventType _idleEvent;
    private readonly EventType _turnEvent;
    private readonly ModelTurn _modelTurn;
    private bool _messageToolCalled = false;
    private ModelTurn? _lastAgentMessage;
    private readonly ChannelMessage _channelMessageTemplate;
    private readonly AgentStopRequest _agentStopRequest;

    public TransientAgent(
        IAgent agent,
        IEventBus eventBus,
        IDataContextScope dataScope,
        ISessionQueue sessionQueue
        )
    {
        var sessionPath = dataScope.GetSessionPath();

        if (sessionPath.IsRootSession)
            throw new InvalidOperationException("A root session cannot be a transient session");
        if (sessionPath.Current.IsDefault)
            throw new InvalidOperationException("A default session cannot be transient");

        _modelTurn =
            dataScope.TryGetValue<TransientAgentInitialPrompt>(TransientAgentInitialPrompt.DataKey, out var prompt)
                ? prompt.Prompt
                : throw new InvalidOperationException("Transient agents require an initial prompt");

        dataScope.Remove(TransientAgentInitialPrompt.DataKey);
        _agent = agent;
        _eventBus = eventBus;
        _sessionQueue = sessionQueue;
        _messageEvent = Event.WellKnown.Channel.Message with { Id = sessionPath.Parent };
        _stopEvent = Event.WellKnown.Command.AgentStop with { Id = sessionPath.Current };
        _idleEvent = Event.WellKnown.Agent.Idle with { Id = sessionPath.Current };
        _turnEvent = Event.WellKnown.Agent.Turn with { Id = sessionPath.Current };
        _channelMessageTemplate = new ChannelMessage("", $"subagent:{sessionPath.Current}", default);
        _agentStopRequest = new AgentStopRequest(sessionPath.Current);
    }

    public async Task RunAsync()
    {
        await using var subscription = Subscribe();
        // TODO: Render the appropriate prompt template (define in agent config)
        await _sessionQueue.EnqueueAsync(_modelTurn, CancellationToken.None);
        await _agent.RunAsync();
        await SendMessageToParentAsync();
    }

    private async ValueTask SendMessageToParentAsync()
    {
        if (_messageToolCalled) return;
        if (_lastAgentMessage is null) return;
        if (string.IsNullOrEmpty(_lastAgentMessage.Content)) return;
        await SendMessageToParentAsync(_lastAgentMessage.Content);
    }

    private async ValueTask SendMessageToParentAsync(string message)
    {
        var channelMessage = _channelMessageTemplate with { Text = message, Timestamp = DateTimeOffset.Now };
        await _eventBus.PublishAsync(_messageEvent, channelMessage, CancellationToken.None);
    }
    public async ValueTask HandleAsync(IEventEnvelope<AgentLifecycleEvent> envelope,
        CancellationToken cancellationToken) => await _eventBus.PublishAsync(_stopEvent, _agentStopRequest, cancellationToken);

    private IAsyncDisposable Subscribe()
    {
        var idleSubscirption = _eventBus.Subscribe<AgentLifecycleEvent>(
            _idleEvent,
            EventDeliveryMode.FireAndForget,
            handler: this,
            preserveSubscriberExecutionContext: true);
        var turnSubscription = _eventBus.Subscribe<ModelTurn>(
            _turnEvent,
            EventDeliveryMode.Awaited,
            handler: this,
            preserveSubscriberExecutionContext: true);

        return idleSubscirption.And(turnSubscription);
    }

    public async ValueTask HandleAsync(IEventEnvelope<ModelTurn> envelope, CancellationToken cancellationToken)
    {
        if(_lastAgentMessage?.Content == Sentinel.NoResponse) return;
        var turn = envelope.Data;
        if (turn is null) return;
        if (turn.Role is ModelRole.Assistant)
            _lastAgentMessage = turn;
        if (turn.Role is ModelRole.Tool)
        {
            if (turn.IsError) return;
            if (turn.ToolCall?.Source != ToolCall.InternalToolSource) return;
            if (turn.ToolCall?.Name != "message_send") return;
            _messageToolCalled = true;
        }
    }
}