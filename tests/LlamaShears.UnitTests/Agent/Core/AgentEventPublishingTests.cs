using LlamaShears.Core.Abstractions.Agent.Persistence;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Agent;
using LlamaShears.Core.Abstractions.Events.Channel;
using LlamaShears.Core.Eventing;
using LlamaShears.Core.Eventing.Extensions;
using LlamaShears.Core.Persistence;
using LlamaShears.Core.Sessions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace LlamaShears.UnitTests.Agent.Core;

public sealed class AgentEventPublishingTests
{
    [Test]
    public async Task TextFragmentsCarryCumulativeContentAndTheFinalFragmentHoldsTheCompleteMessage()
    {
        var captured = await RunSingleTurnAsync(
            agentId: "alice",
            model: ScriptedLanguageModel.WithText("Hi", " there"));

        var fragments = captured
            .Where(c => c.Type.Component == Event.Sources.Agent && c.Type.EventName == "message")
            .Select(c => c.Data)
            .OfType<AgentMessageFragment>()
            .ToArray();

        await Assert.That(fragments).Count().IsEqualTo(3);
        await Assert.That(fragments.Select(f => f.Content).ToArray())
            .IsEquivalentTo(["Hi", "Hi there", "Hi there"]);
        await Assert.That(fragments.Select(f => f.Final).ToArray())
            .IsEquivalentTo([false, false, true]);
        await Assert.That(fragments[^1].Content).IsEqualTo("Hi there");
    }

    [Test]
    public async Task ThoughtFragmentsCarryCumulativeContentAndTheFinalFragmentHoldsTheCompleteThought()
    {
        var captured = await RunSingleTurnAsync(
            agentId: "alice",
            model: ScriptedLanguageModel.WithThoughtThenText(
                ["thinking", "..."],
                ["done"]));

        var fragments = captured
            .Where(c => c.Type.Component == Event.Sources.Agent && c.Type.EventName == "thought")
            .Select(c => c.Data)
            .OfType<AgentThoughtFragment>()
            .ToArray();

        await Assert.That(fragments).Count().IsEqualTo(3);
        await Assert.That(fragments.Select(f => f.Content).ToArray())
            .IsEquivalentTo(["thinking", "thinking...", "thinking..."]);
        await Assert.That(fragments.Select(f => f.Final).ToArray())
            .IsEquivalentTo([false, false, true]);
        await Assert.That(fragments[^1].Content).IsEqualTo("thinking...");
    }

    [Test]
    public async Task EveryFragmentEventEmittedDuringATurnSharesTheSameCorrelationId()
    {
        var captured = await RunSingleTurnAsync(
            agentId: "alice",
            model: ScriptedLanguageModel.WithThoughtThenText(["thinking"], ["done"]));

        var fragmentEvents = captured
            .Where(c => c.Data is AgentMessageFragment or AgentThoughtFragment)
            .ToArray();
        var correlationIds = fragmentEvents.Select(c => c.CorrelationId).Distinct().ToArray();

        await Assert.That(correlationIds).Count().IsEqualTo(1);
        await Assert.That(correlationIds[0]).IsNotEqualTo(Guid.Empty);
    }

    [Test]
    public async Task FragmentEventTypesCarryTheSessionInTheirIdSegment()
    {
        var captured = await RunSingleTurnAsync(
            agentId: "alice",
            model: ScriptedLanguageModel.WithText("hi"));

        var fragmentEvents = captured
            .Where(c => c.Data is AgentMessageFragment or AgentThoughtFragment)
            .ToArray();
        var ids = fragmentEvents.Select(c => c.Type.Id).Distinct().ToArray();

        await Assert.That(ids).Count().IsEqualTo(1);
        await Assert.That(SessionId.TryParse(ids[0]!, out var parsed)).IsTrue();
        await Assert.That(parsed!.AgentId).IsEqualTo("alice");
        await Assert.That(parsed.IsDefault).IsTrue();
    }

    [Test]
    public async Task ATurnWithNoTextProducesNoMessageFragmentEvents()
    {
        var captured = await RunSingleTurnAsync(
            agentId: "alice",
            model: ScriptedLanguageModel.WithThoughtThenText(["just thinking"], []));

        var messageFragments = captured
            .Select(c => c.Data)
            .OfType<AgentMessageFragment>()
            .ToArray();
        await Assert.That(messageFragments).IsEmpty();
    }

    private static async Task<IReadOnlyList<CapturedEvent>> RunSingleTurnAsync(
        string agentId,
        ScriptedLanguageModel model)
    {
        await using var provider = BuildServices();
        var capturing = new CapturingEventPublisher(provider.GetRequiredService<IEventBus>());
        var bus = provider.GetRequiredService<IEventBus>();
        var session = new SessionId(agentId, SessionId.DefaultSessionName);
        var ctx = await provider.GetRequiredService<IContextStore>().OpenAsync(session, CancellationToken.None);

        using var captureChannel = new CapturingTurnSubscriber(bus, session);
        await using var agent = await AgentHarness.StartAsync(
            agentId,
            session,
            provider,
            ctx,
            model,
            eventBus: capturing);

        await capturing.PublishAsync(
            Event.WellKnown.Channel.Message with { Id = session },
            new ChannelMessage("hello", "test", DateTimeOffset.UtcNow),
            CancellationToken.None);
        await captureChannel.WaitForTurnAsync(TimeSpan.FromMilliseconds(500));

        return capturing.Captured;
    }

    private static ServiceProvider BuildServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEventingFramework();
        services.AddSingleton<IContextStore>(new FakeContextStore());
        services.AddSingleton(Substitute.For<IDataContextFactory>());
        services.AddEventHandler<AgentTurnContextPersister>();
        services.AddSingleton<ISessionFactory, SessionFactory>();
        var provider = services.BuildServiceProvider();
        provider.GetRequiredService<AgentTurnContextPersister>();
        return provider;
    }

    /// <summary>
    /// Records every published event AND forwards to the real bus so the
    /// activated persister still sees turns. Without forwarding, the agent's
    /// agent:turn events would be swallowed and the agent's own context
    /// growth check would never trigger model invocation.
    /// </summary>
    private sealed class CapturingEventPublisher : IEventBus
    {
        private readonly IEventBus _inner;
        private readonly List<CapturedEvent> _captured = [];
        private readonly Lock _gate = new Lock();

        public CapturingEventPublisher(IEventBus inner)
        {
            _inner = inner;
        }

        public IReadOnlyList<CapturedEvent> Captured
        {
            get
            {
                lock (_gate)
                {
                    return [.. _captured];
                }
            }
        }

        public IDisposable Subscribe<T>(string? pattern, EventDeliveryMode mode, IEventHandler<T> handler, bool preserveSubscriberExecutionContext = false) where T : class
        {
            return _inner.Subscribe(pattern, mode, handler, preserveSubscriberExecutionContext);
        }

        public async ValueTask PublishAsync<T>(EventType eventType, T? data, Guid correlationId, CancellationToken cancellationToken)
            where T : class
        {
            lock (_gate)
            {
                _captured.Add(new CapturedEvent(eventType, data, correlationId));
            }
            await _inner.PublishAsync(eventType, data, correlationId, cancellationToken);
        }
    }

    internal sealed record CapturedEvent(EventType Type, object? Data, Guid CorrelationId);
}
