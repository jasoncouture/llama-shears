using LlamaShears.Core;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Agent;
using LlamaShears.Core.Abstractions.Events.Channel;
using LlamaShears.Core.Abstractions.Provider;
using NSubstitute;
using NSubstitute.Extensions;

namespace LlamaShears.UnitTests.Agent.Core;

public sealed class TransientAgentTests
{
    private static readonly DateTimeOffset _now = DateTimeOffset.UnixEpoch;

    private TransientAgent _agent = null!;
    private IAgent _inner = null!;
    private IEventBus _bus = null!;
    private FakeDataContextScope _scope = null!;
    private ISessionQueue _queue = null!;
    private SessionPath _path = null!;

    [Test]
    public async Task ConstructorThrowsWhenSessionIsRoot()
    {
        var rootPath = new SessionPath(new SessionId("agent-a", "child"));
        var scope = new FakeDataContextScope(rootPath.Current);
        ((IDataContextScope)scope).SetItem(SessionPath.DataKey, rootPath);
        ((IDataContextScope)scope).SetItem(TransientAgentInitialPrompt.DataKey, new TransientAgentInitialPrompt(SamplePrompt()));

        await Assert.That(() => new TransientAgent(
                Substitute.For<IAgent>(),
                Substitute.For<IEventBus>(),
                scope,
                Substitute.For<ISessionQueue>()))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task ConstructorThrowsWhenChildSessionNameIsDefault()
    {
        var rootPath = new SessionPath(new SessionId("agent-a", "channel"));
        var childPath = rootPath.CreateChildSession(new SessionId("agent-a", SessionId.DefaultSessionName));
        var scope = new FakeDataContextScope(childPath.Current);
        ((IDataContextScope)scope).SetItem(SessionPath.DataKey, childPath);
        ((IDataContextScope)scope).SetItem(TransientAgentInitialPrompt.DataKey, new TransientAgentInitialPrompt(SamplePrompt()));

        await Assert.That(() => new TransientAgent(
                Substitute.For<IAgent>(),
                Substitute.For<IEventBus>(),
                scope,
                Substitute.For<ISessionQueue>()))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task ConstructorThrowsWhenInitialPromptMissing()
    {
        var path = BuildChildPath();
        var scope = new FakeDataContextScope(path.Current);
        ((IDataContextScope)scope).SetItem(SessionPath.DataKey, path);

        await Assert.That(() => new TransientAgent(
                Substitute.For<IAgent>(),
                Substitute.For<IEventBus>(),
                scope,
                Substitute.For<ISessionQueue>()))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task ConstructorRemovesPromptFromDataScope()
    {
        Setup();

        var present = _scope.TryGetValue<TransientAgentInitialPrompt>(
            TransientAgentInitialPrompt.DataKey,
            out _);

        await Assert.That(present).IsFalse();
    }

    [Test]
    public async Task RunAsyncEnqueuesInitialPromptAndRunsInnerAgent()
    {
        var prompt = SamplePrompt("kick");
        Setup(prompt);

        await _agent.RunAsync();

        await _queue.Received(1).EnqueueAsync(
            Arg.Is<ModelTurn>(t => ReferenceEquals(t, prompt)),
            Arg.Any<CancellationToken>());
        await _inner.Received(1).RunAsync();
    }

    [Test]
    public async Task RunAsyncSubscribesToIdleAndTurnEvents()
    {
        Setup();
        var expectedIdle = Event.WellKnown.Agent.Idle with { Id = _path.Current };
        var expectedTurn = Event.WellKnown.Agent.Turn with { Id = _path.Current };

        await _agent.RunAsync();
        
        _bus.Received(1).Subscribe<AgentLifecycleEvent>(
            Arg.Is<string>(expectedIdle),
            Arg.Is(EventDeliveryMode.FireAndForget),
            Arg.Is(_agent),
            Arg.Is(true));
        _bus.Received(1).Subscribe<ModelTurn>(
            Arg.Is<string>(expectedTurn),
            Arg.Is(EventDeliveryMode.Awaited),
            Arg.Is(_agent),
            Arg.Is(true));
    }

    [Test]
    public async Task IdleHandlerPublishesAgentStopForCurrentSession()
    {
        Setup();
        var lifecycle = new AgentLifecycleEvent(
            TestAgentConfigs.WithHeartbeat(TimeSpan.Zero, id: _path.Current.AgentId),
            _path.Current);

        await _agent.HandleAsync(Envelope(lifecycle), CancellationToken.None);

        await _bus.Received(1).PublishAsync(
            Arg.Is<EventType>(t => t.Component == Event.WellKnown.Command.AgentStop.Component
                && t.EventName == Event.WellKnown.Command.AgentStop.EventName
                && t.Id == _path.Current.ToString()),
            Arg.Is<AgentStopRequest>(r => r.SessionId == _path.Current),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AssistantTurnIsForwardedToParentOnCompletion()
    {
        Setup();
        var assistant = new ModelTurn(ModelRole.Assistant, "final answer", _now);

        await _agent.HandleAsync(Envelope(assistant), CancellationToken.None);
        await _agent.RunAsync();

        await _bus.Received(1).PublishAsync(
            Arg.Is<EventType>(t => t.Component == Event.WellKnown.Channel.Message.Component
                && t.EventName == Event.WellKnown.Channel.Message.EventName
                && t.Id == _path.Parent.ToString()),
            Arg.Is<ChannelMessage>(m => m.Text == "final answer"
                && m.ChannelId == $"subagent:{_path.Current}"),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task MessageSendToolSuppressesAutoForward()
    {
        Setup();
        var assistant = new ModelTurn(ModelRole.Assistant, "would be auto-sent", _now);
        var toolTurn = new ModelTurn(ModelRole.Tool, "ok", _now)
        {
            ToolCall = new ToolCall(ToolCall.InternalToolSource, "message_send", "{}", CallId: "1"),
        };

        await _agent.HandleAsync(Envelope(assistant), CancellationToken.None);
        await _agent.HandleAsync(Envelope(toolTurn), CancellationToken.None);
        await _agent.RunAsync();

        await _bus.DidNotReceive().PublishAsync(
            Arg.Is<EventType>(t => t.Component == Event.WellKnown.Channel.Message.Component),
            Arg.Any<ChannelMessage>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ErrorToolDoesNotSuppressAutoForward()
    {
        Setup();
        var assistant = new ModelTurn(ModelRole.Assistant, "answer", _now);
        var toolTurn = new ModelTurn(ModelRole.Tool, "boom", _now)
        {
            ToolCall = new ToolCall(ToolCall.InternalToolSource, "message_send", "{}", CallId: "1"),
            IsError = true,
        };

        await _agent.HandleAsync(Envelope(assistant), CancellationToken.None);
        await _agent.HandleAsync(Envelope(toolTurn), CancellationToken.None);
        await _agent.RunAsync();

        await _bus.Received(1).PublishAsync(
            Arg.Is<EventType>(t => t.Component == Event.WellKnown.Channel.Message.Component),
            Arg.Is<ChannelMessage>(m => m.Text == "answer"),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ToolFromNonInternalSourceDoesNotSuppressAutoForward()
    {
        Setup();
        var assistant = new ModelTurn(ModelRole.Assistant, "answer", _now);
        var toolTurn = new ModelTurn(ModelRole.Tool, "ok", _now)
        {
            ToolCall = new ToolCall("other_mcp", "message_send", "{}", CallId: "1"),
        };

        await _agent.HandleAsync(Envelope(assistant), CancellationToken.None);
        await _agent.HandleAsync(Envelope(toolTurn), CancellationToken.None);
        await _agent.RunAsync();

        await _bus.Received(1).PublishAsync(
            Arg.Is<EventType>(t => t.Component == Event.WellKnown.Channel.Message.Component),
            Arg.Is<ChannelMessage>(m => m.Text == "answer"),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ToolWithDifferentNameDoesNotSuppressAutoForward()
    {
        Setup();
        var assistant = new ModelTurn(ModelRole.Assistant, "answer", _now);
        var toolTurn = new ModelTurn(ModelRole.Tool, "ok", _now)
        {
            ToolCall = new ToolCall(ToolCall.InternalToolSource, "memory_store", "{}", CallId: "1"),
        };

        await _agent.HandleAsync(Envelope(assistant), CancellationToken.None);
        await _agent.HandleAsync(Envelope(toolTurn), CancellationToken.None);
        await _agent.RunAsync();

        await _bus.Received(1).PublishAsync(
            Arg.Is<EventType>(t => t.Component == Event.WellKnown.Channel.Message.Component),
            Arg.Is<ChannelMessage>(m => m.Text == "answer"),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task NoAssistantTurnMeansNoAutoForward()
    {
        Setup();

        await _agent.RunAsync();

        await _bus.DidNotReceive().PublishAsync(
            Arg.Is<EventType>(t => t.Component == Event.WellKnown.Channel.Message.Component),
            Arg.Any<ChannelMessage>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task OnlyMostRecentAssistantTurnIsForwarded()
    {
        Setup();
        var first = new ModelTurn(ModelRole.Assistant, "first", _now);
        var second = new ModelTurn(ModelRole.Assistant, "second", _now.AddSeconds(1));

        await _agent.HandleAsync(Envelope(first), CancellationToken.None);
        await _agent.HandleAsync(Envelope(second), CancellationToken.None);
        await _agent.RunAsync();

        await _bus.Received(1).PublishAsync(
            Arg.Is<EventType>(t => t.Component == Event.WellKnown.Channel.Message.Component),
            Arg.Is<ChannelMessage>(m => m.Text == "second"),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    private void Setup(ModelTurn? prompt = null, SessionPath? path = null)
    {
        _path = path ?? BuildChildPath();
        prompt ??= SamplePrompt();
        _scope = new FakeDataContextScope(_path.Current);
        ((IDataContextScope)_scope).SetItem(SessionPath.DataKey, _path);
        ((IDataContextScope)_scope).SetItem(TransientAgentInitialPrompt.DataKey, new TransientAgentInitialPrompt(prompt));

        _inner = Substitute.For<IAgent>();
        _inner.RunAsync().Returns(Task.CompletedTask);
        _bus = Substitute.For<IEventBus>();
        _queue = Substitute.For<ISessionQueue>();

        _agent = new TransientAgent(_inner, _bus, _scope, _queue);
    }

    private static ModelTurn SamplePrompt(string text = "kick") =>
        new ModelTurn(ModelRole.User, text, _now);

    private static SessionPath BuildChildPath()
    {
        var root = new SessionPath(new SessionId("agent-a", "channel"));
        return root.CreateChildSession(new SessionId("agent-a", "transient"));
    }

    private IEventEnvelope<T> Envelope<T>(T data) where T : class
    {
        var envelope = Substitute.For<IEventEnvelope<T>>();
        envelope.Data.Returns(data);
        envelope.DeliveryMode.Returns(EventDeliveryMode.Awaited);
        envelope.CorrelationId.Returns(Guid.NewGuid());
        return envelope;
    }
}
