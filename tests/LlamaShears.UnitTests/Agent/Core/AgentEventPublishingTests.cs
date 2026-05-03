using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Events;
using LlamaShears.Core.Abstractions.Context;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Agent;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Core.Channels;
using LlamaShears.Core.Eventing;
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
    public async Task TextFragmentsCarryCumulativeContentAndTheFinalFragmentHoldsTheCompleteMessage()
    {
        var publisher = new CapturingEventPublisher();
        await RunSingleTurnAsync(
            agentId: "alice",
            model: ScriptedLanguageModel.WithText("Hi", " there"),
            publisher: publisher);

        var fragments = publisher.Captured
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
        var publisher = new CapturingEventPublisher();
        await RunSingleTurnAsync(
            agentId: "alice",
            model: ScriptedLanguageModel.WithThoughtThenText(
                ["thinking", "..."],
                ["done"]),
            publisher: publisher);

        var fragments = publisher.Captured
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
    public async Task ATurnWithNoTextProducesNoMessageFragmentEvents()
    {
        var publisher = new CapturingEventPublisher();
        await RunSingleTurnAsync(
            agentId: "alice",
            model: ScriptedLanguageModel.WithThoughtThenText(["just thinking"], []),
            publisher: publisher);

        var messageFragments = publisher.Captured
            .Where(c => c.Type.Component == Event.Sources.Agent && c.Type.EventName == "message")
            .Select(c => c.Data)
            .OfType<AgentMessageFragment>()
            .ToArray();
        await Assert.That(messageFragments).IsEmpty();
    }

    private static async Task RunSingleTurnAsync(
        string agentId,
        ScriptedLanguageModel model,
        CapturingEventPublisher publisher)
    {
        await using var provider = BuildServices();
        var tickPublisher = provider.GetRequiredService<IEventPublisher>();
        var bus = provider.GetRequiredService<IEventBus>();
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
            bus: bus,
            systemPromptProvider: new HardcodedSystemPromptProvider(TimeProvider.System),
            timeProvider: new FakeTimeProvider(DateTimeOffset.UnixEpoch),
            compactor: BuildNoOpCompactor(),
            modelConfiguration: new ModelConfiguration("test"),
            agentContextProvider: BuildContextProvider(agentId),
            fragments: fragmentPublisher,
            eventPublisher: publisher);

        await tickPublisher.PublishAsync(
            Event.WellKnown.Host.Tick,
            new SystemTick(DateTimeOffset.UtcNow),
            CancellationToken.None);
        await captureChannel.WaitForTurnAsync(TimeSpan.FromSeconds(5));
    }

    private static ServiceProvider BuildServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEventingFramework();
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
