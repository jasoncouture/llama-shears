using LlamaShears.Core;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Channel;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.UnitTests.Agent.Pipeline;
using NSubstitute;

namespace LlamaShears.UnitTests.Agent.Core;

public sealed class ChannelMessageIntakeServiceTests
{
    [Test]
    public async Task HandleAsyncEnqueuesAUserTurn()
    {
        var queue = Substitute.For<ISessionQueue>();
        var bus = Substitute.For<IEventBus>();
        IAgentService service = new ChannelMessageIntakeService(
            bus,
            queue,
            PipelineTestContext.ScopeFor());
        var message = new ChannelMessage("hello", "web", DateTimeOffset.UnixEpoch);
        var envelope = Envelope(message);

        await ((IEventHandler<ChannelMessage>)service).HandleAsync(envelope, CancellationToken.None);

        await queue.Received(1).EnqueueAsync(
            Arg.Is<ModelTurn>(t =>
                t.Role == ModelRole.User
                && t.Content == "hello"
                && t.ChannelId == "web"),
            CancellationToken.None);
    }

    [Test]
    public async Task HandleAsyncIgnoresNullPayload()
    {
        var queue = Substitute.For<ISessionQueue>();
        IAgentService service = new ChannelMessageIntakeService(
            Substitute.For<IEventBus>(),
            queue,
            PipelineTestContext.ScopeFor());

        await ((IEventHandler<ChannelMessage>)service).HandleAsync(Envelope<ChannelMessage>(null), CancellationToken.None);

        await queue.DidNotReceive().EnqueueAsync(Arg.Any<ModelTurn>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task StartAsyncSubscribesToThisSessionOnly()
    {
        var bus = Substitute.For<IEventBus>();
        var subscription = Substitute.For<IDisposable>();
        bus.Subscribe(
                Arg.Any<string?>(),
                Arg.Any<EventDeliveryMode>(),
                Arg.Any<IEventHandler<ChannelMessage>>(),
                Arg.Any<bool>())
            .Returns(subscription);
        var scope = PipelineTestContext.ScopeFor("alice");
        var session = scope.GetCurrentSessionId();
        string? expectedPattern = Event.WellKnown.Channel.Message with { Id = session };
        IAgentService service = new ChannelMessageIntakeService(
            bus,
            Substitute.For<ISessionQueue>(),
            scope);

        await service.StartAsync(CancellationToken.None);

        bus.Received(1).Subscribe(
            expectedPattern,
            EventDeliveryMode.Awaited,
            Arg.Any<IEventHandler<ChannelMessage>>(),
            true);

        await service.StopAsync(CancellationToken.None);
        subscription.Received(1).Dispose();
    }

    private static IEventEnvelope<T> Envelope<T>(T? data) where T : class
    {
        var envelope = Substitute.For<IEventEnvelope<T>>();
        envelope.Data.Returns(data);
        return envelope;
    }
}
