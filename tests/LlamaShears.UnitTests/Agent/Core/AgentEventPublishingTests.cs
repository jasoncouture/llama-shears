using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Events;
using LlamaShears.Core.Abstractions.Context;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Agent;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Core.Channels;
using LlamaShears.Core.SystemPrompt;
using MessagePipe;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace LlamaShears.UnitTests.Agent.Core;

public sealed class AgentEventPublishingTests
{
    [Test]
    public async Task TextStreamPublishesAFragmentEventPerChunkAFinalEmptyFragmentAndAnAggregateMessage()
    {
        var publisher = new CapturingEventPublisher();
        await RunSingleTurnAsync(
            agentId: "alice",
            model: ScriptedLanguageModel.WithText("Hi", " there"),
            publisher: publisher);

        var messageEvents = publisher.Captured
            .Where(c => c.Type.Component == Event.Sources.Agent && c.Type.EventName == "message")
            .ToArray();

        var fragments = messageEvents.Select(c => c.Data).OfType<AgentMessageFragment>().ToArray();
        await Assert.That(fragments).Count().IsEqualTo(3);
        await Assert.That(fragments.Select(f => f.Text).ToArray())
            .IsEquivalentTo(["Hi", " there", string.Empty]);
        await Assert.That(fragments.Select(f => f.Final).ToArray())
            .IsEquivalentTo([false, false, true]);

        var aggregates = messageEvents.Select(c => c.Data).OfType<AgentMessage>().ToArray();
        await Assert.That(aggregates).Count().IsEqualTo(1);
        await Assert.That(aggregates[0].Text).IsEqualTo("Hi there");
    }

    [Test]
    public async Task ThoughtStreamPublishesAFragmentEventPerChunkAFinalEmptyFragmentAndAnAggregateThought()
    {
        var publisher = new CapturingEventPublisher();
        await RunSingleTurnAsync(
            agentId: "alice",
            model: ScriptedLanguageModel.WithThoughtThenText(
                ["thinking", "..."],
                ["done"]),
            publisher: publisher);

        var thoughtEvents = publisher.Captured
            .Where(c => c.Type.Component == Event.Sources.Agent && c.Type.EventName == "thought")
            .ToArray();

        var fragments = thoughtEvents.Select(c => c.Data).OfType<AgentThoughtFragment>().ToArray();
        await Assert.That(fragments).Count().IsEqualTo(3);
        await Assert.That(fragments.Select(f => f.Text).ToArray())
            .IsEquivalentTo(["thinking", "...", string.Empty]);
        await Assert.That(fragments.Select(f => f.Final).ToArray())
            .IsEquivalentTo([false, false, true]);

        var aggregates = thoughtEvents.Select(c => c.Data).OfType<AgentThought>().ToArray();
        await Assert.That(aggregates).Count().IsEqualTo(1);
        await Assert.That(aggregates[0].Text).IsEqualTo("thinking...");
    }

    [Test]
    public async Task EveryEventEmittedDuringATurnSharesTheSameCorrelationId()
    {
        var publisher = new CapturingEventPublisher();
        await RunSingleTurnAsync(
            agentId: "alice",
            model: ScriptedLanguageModel.WithThoughtThenText(["thinking"], ["done"]),
            publisher: publisher);

        var correlationIds = publisher.Captured.Select(c => c.CorrelationId).Distinct().ToArray();
        await Assert.That(correlationIds).Count().IsEqualTo(1);
        await Assert.That(correlationIds[0]).IsNotEqualTo(Guid.Empty);
    }

    [Test]
    public async Task EventTypeCarriesTheAgentIdInItsIdSegment()
    {
        var publisher = new CapturingEventPublisher();
        await RunSingleTurnAsync(
            agentId: "alice",
            model: ScriptedLanguageModel.WithText("hi"),
            publisher: publisher);

        var ids = publisher.Captured.Select(c => c.Type.Id).Distinct().ToArray();
        await Assert.That(ids).Count().IsEqualTo(1);
        await Assert.That(ids[0]).IsEqualTo("alice");
    }

    [Test]
    public async Task ATurnWithNoTextProducesNoMessageAggregateEvent()
    {
        var publisher = new CapturingEventPublisher();
        await RunSingleTurnAsync(
            agentId: "alice",
            model: ScriptedLanguageModel.WithThoughtThenText(["just thinking"], []),
            publisher: publisher);

        var aggregates = publisher.Captured
            .Where(c => c.Type.Component == Event.Sources.Agent && c.Type.EventName == "message")
            .Select(c => c.Data)
            .OfType<AgentMessage>()
            .ToArray();
        await Assert.That(aggregates).IsEmpty();
    }

    private static async Task RunSingleTurnAsync(
        string agentId,
        ScriptedLanguageModel model,
        CapturingEventPublisher publisher)
    {
        await using var provider = BuildServices();
        var tickPublisher = provider.GetRequiredService<IAsyncPublisher<SystemTick>>();
        var ticks = provider.GetRequiredService<IAsyncSubscriber<SystemTick>>();
        var fragmentPublisher = provider.GetRequiredService<IAsyncPublisher<AgentFragmentEmitted>>();

        var captureChannel = new CapturingOutputChannel();
        var seed = new SeedInputChannel([
            new ModelTurn(ModelRole.User, "hello", DateTimeOffset.UtcNow),
        ]);

        using var agent = new global::LlamaShears.Core.Agent(
            id: agentId,
            config: TestAgentConfigs.WithHeartbeat(TimeSpan.Zero),
            model: model,
            agentContext: new FakeAgentContext(agentId),
            inputChannels: [seed],
            outputChannels: [captureChannel],
            loggerFactory: NullLoggerFactory.Instance,
            ticks: ticks,
            systemPromptProvider: new HardcodedSystemPromptProvider(TimeProvider.System),
            timeProvider: new FakeTimeProvider(DateTimeOffset.UnixEpoch),
            compactor: BuildNoOpCompactor(),
            modelConfiguration: new ModelConfiguration("test"),
            agentContextProvider: BuildContextProvider(agentId),
            fragments: fragmentPublisher,
            eventPublisher: publisher);

        await tickPublisher.PublishAsync(new SystemTick(DateTimeOffset.UtcNow), CancellationToken.None);
        await captureChannel.WaitForTurnAsync(TimeSpan.FromSeconds(5));
    }

    private static ServiceProvider BuildServices()
    {
        var services = new ServiceCollection();
        services.AddMessagePipe();
        return services.BuildServiceProvider();
    }

    private static IContextCompactor BuildNoOpCompactor()
    {
        var compactor = Substitute.For<IContextCompactor>();
        compactor.CompactAsync(
                Arg.Any<AgentContext>(),
                Arg.Any<ModelPrompt>(),
                Arg.Any<ILanguageModel>(),
                Arg.Any<ModelConfiguration>(),
                Arg.Any<CancellationToken>())
            .Returns(call => ValueTask.FromResult(call.Arg<ModelPrompt>()));
        return compactor;
    }

    private static IAgentContextProvider BuildContextProvider(string agentId)
    {
        var contextProvider = Substitute.For<IAgentContextProvider>();
        contextProvider.CreateAgentContextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<AgentContext?>(TestAgentConfigs.BuildAgentContext(agentId)));
        return contextProvider;
    }

    private sealed class CapturingEventPublisher : IEventPublisher
    {
        private readonly List<CapturedEvent> _captured = [];
        private readonly Lock _gate = new();

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

        public ValueTask PublishAsync<T>(EventType eventType, T? data, Guid correlationId, CancellationToken cancellationToken)
            where T : class
        {
            lock (_gate)
            {
                _captured.Add(new CapturedEvent(eventType, data, correlationId));
            }
            return ValueTask.CompletedTask;
        }
    }

    private sealed record CapturedEvent(EventType Type, object? Data, Guid CorrelationId);
}
