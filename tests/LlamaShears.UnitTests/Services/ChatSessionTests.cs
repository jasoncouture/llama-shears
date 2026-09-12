using LlamaShears.Api.Web.Services;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Commands;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Agent;
using LlamaShears.Core.Abstractions.Provider;
using NSubstitute;

namespace LlamaShears.UnitTests.Services;

public sealed class ChatSessionTests
{
    [Test]
    public async Task SendAsyncMarksTheSessionProcessingBeforeTheAgentIsBusy()
    {
        var fixture = await Fixture.CreateAsync();

        await fixture.Session.SendAsync("hello", CancellationToken.None);

        await Assert.That(fixture.Session.IsProcessing).IsTrue();
        await Assert.That(fixture.Session.Bubbles.Count).IsEqualTo(1);
    }

    [Test]
    public async Task IdleClearsProcessing()
    {
        var fixture = await Fixture.CreateAsync();
        await fixture.Session.SendAsync("hello", CancellationToken.None);

        await fixture.PublishLifecycleAsync(Event.WellKnown.Agent.Idle);

        await Assert.That(fixture.Session.IsProcessing).IsFalse();
    }

    [Test]
    public async Task BusyMarksProcessingWithoutAUserSend()
    {
        var fixture = await Fixture.CreateAsync();

        await fixture.PublishLifecycleAsync(Event.WellKnown.Agent.Busy);

        await Assert.That(fixture.Session.IsProcessing).IsTrue();
        await Assert.That(fixture.Session.Bubbles).IsEmpty();
    }

    [Test]
    public async Task SlashCommandDoesNotMarkProcessing()
    {
        var command = Substitute.For<ISlashCommand>();
        command.ExecuteAsync(Arg.Any<SlashCommandContext>(), Arg.Any<CancellationToken>())
            .Returns(SlashCommandResult.Default);
        var fixture = await Fixture.CreateAsync(slash => slash.Find("/ping").Returns(command));

        await fixture.Session.SendAsync("/ping", CancellationToken.None);

        await Assert.That(fixture.Session.IsProcessing).IsFalse();
        await Assert.That(fixture.Session.Bubbles).IsEmpty();
        await command.Received(1).ExecuteAsync(Arg.Any<SlashCommandContext>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SelectSessionClearsProcessing()
    {
        var fixture = await Fixture.CreateAsync();
        await fixture.Session.SendAsync("hello", CancellationToken.None);
        var other = SessionId.CreateFor("bob");

        await fixture.Session.SelectSessionAsync(other, CancellationToken.None);

        await Assert.That(fixture.Session.IsProcessing).IsFalse();
    }

    [Test]
    public async Task IdleForAnotherSessionLeavesProcessingSet()
    {
        var fixture = await Fixture.CreateAsync();
        await fixture.Session.SendAsync("hello", CancellationToken.None);
        var other = SessionId.CreateFor("bob");

        await fixture.Bus.PublishAsync(
            Event.WellKnown.Agent.Idle with { Id = other },
            (AgentLifecycleEvent?)null,
            Guid.CreateVersion7(),
            CancellationToken.None);

        await Assert.That(fixture.Session.IsProcessing).IsTrue();
    }

    [Test]
    public async Task FailedPublishClearsProcessing()
    {
        var directory = Substitute.For<IAgentDirectory>();
        directory.GetTurnsAsync(Arg.Any<SessionId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ModelTurn>>([]));
        var slash = Substitute.For<ISlashCommandRegistry>();
        slash.Find(Arg.Any<string>()).Returns((ISlashCommand?)null);
        IChatSession session = new ChatSession(
            new RecordingEventBus(),
            new ThrowingPublishBus(),
            directory,
            slash);
        await session.SelectSessionAsync(SessionId.CreateFor("alice"), CancellationToken.None);

        var threw = false;
        try
        {
            await session.SendAsync("hello", CancellationToken.None);
        }
        catch (InvalidOperationException)
        {
            threw = true;
        }

        await Assert.That(threw).IsTrue();
        await Assert.That(session.IsProcessing).IsFalse();
        session.Dispose();
    }

    [Test]
    public async Task ChangedFiresWhenProcessingStartsAndStops()
    {
        var fixture = await Fixture.CreateAsync();
        var changes = 0;
        fixture.Session.Changed += () => changes++;

        await fixture.Session.SendAsync("hello", CancellationToken.None);
        var afterSend = changes;
        await fixture.PublishLifecycleAsync(Event.WellKnown.Agent.Idle);

        await Assert.That(afterSend).IsGreaterThanOrEqualTo(1);
        await Assert.That(changes).IsGreaterThan(afterSend);
    }

    private sealed class Fixture
    {
        private Fixture(RecordingEventBus bus, IChatSession session, SessionId sessionId)
        {
            Bus = bus;
            Session = session;
            SessionId = sessionId;
        }

        public RecordingEventBus Bus { get; }
        public IChatSession Session { get; }
        public SessionId SessionId { get; }

        public static Task<Fixture> CreateAsync(Action<ISlashCommandRegistry>? configureSlash = null)
        {
            var bus = new RecordingEventBus();
            var directory = Substitute.For<IAgentDirectory>();
            directory.GetTurnsAsync(Arg.Any<SessionId>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<ModelTurn>>([]));
            var slash = Substitute.For<ISlashCommandRegistry>();
            if (configureSlash is null)
            {
                slash.Find(Arg.Any<string>()).Returns((ISlashCommand?)null);
            }
            else
            {
                configureSlash(slash);
            }
            IChatSession session = new ChatSession(bus, bus, directory, slash);
            var sessionId = SessionId.CreateFor("alice");
            return CreateSelectedAsync(bus, session, sessionId);
        }

        public ValueTask PublishLifecycleAsync(EventType type)
            => Bus.PublishAsync(
                type with { Id = SessionId },
                (AgentLifecycleEvent?)null,
                Guid.CreateVersion7(),
                CancellationToken.None);

        private static async Task<Fixture> CreateSelectedAsync(
            RecordingEventBus bus,
            IChatSession session,
            SessionId sessionId)
        {
            await session.SelectSessionAsync(sessionId, CancellationToken.None);
            return new Fixture(bus, session, sessionId);
        }
    }

    private sealed class RecordingEventBus : IEventBus
    {
        private readonly List<Subscription> _subscriptions = [];

        public IDisposable Subscribe<T>(
            string? pattern,
            EventDeliveryMode mode,
            IEventHandler<T> handler,
            bool preserveSubscriberExecutionContext = false)
            where T : class
        {
            var subscription = new Subscription(pattern, typeof(T), handler);
            _subscriptions.Add(subscription);
            return new SubscriptionHandle(() => _subscriptions.Remove(subscription));
        }

        public async ValueTask PublishAsync<T>(
            EventType eventType,
            T? data,
            Guid correlationId,
            CancellationToken cancellationToken)
            where T : class
        {
            var envelope = Substitute.For<IEventEnvelope<T>>();
            envelope.Type.Returns(eventType);
            envelope.Data.Returns(data);
            envelope.CorrelationId.Returns(correlationId);
            envelope.DeliveryMode.Returns(EventDeliveryMode.Awaited);
            var key = eventType.ToString();
            foreach (var subscription in _subscriptions.ToArray())
            {
                if (subscription.PayloadType != typeof(T))
                {
                    continue;
                }

                if (subscription.Pattern is not null
                    && !string.Equals(subscription.Pattern, key, StringComparison.Ordinal))
                {
                    continue;
                }

                if (subscription.Handler is IEventHandler<T> handler)
                {
                    await handler.HandleAsync(envelope, cancellationToken);
                }
            }
        }

        private sealed record Subscription(string? Pattern, Type PayloadType, object Handler);
    }

    private sealed class SubscriptionHandle : IDisposable
    {
        private Action? _dispose;

        public SubscriptionHandle(Action dispose)
        {
            _dispose = dispose;
        }

        public void Dispose()
        {
            var dispose = Interlocked.Exchange(ref _dispose, null);
            dispose?.Invoke();
        }
    }

    private sealed class ThrowingPublishBus : IEventBus
    {
        public IDisposable Subscribe<T>(
            string? pattern,
            EventDeliveryMode mode,
            IEventHandler<T> handler,
            bool preserveSubscriberExecutionContext = false)
            where T : class
            => throw new NotSupportedException();

        public ValueTask PublishAsync<T>(
            EventType eventType,
            T? data,
            Guid correlationId,
            CancellationToken cancellationToken)
            where T : class
            => throw new InvalidOperationException("publish failed");
    }
}
