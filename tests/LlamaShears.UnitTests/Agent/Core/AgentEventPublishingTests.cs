using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Events;
using LlamaShears.Core.Abstractions.Agent.Persistence;
using LlamaShears.Core.Abstractions.Context;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Agent;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Core.Channels;
using LlamaShears.Core.Eventing;
using LlamaShears.Core.Eventing.Extensions;
using LlamaShears.Core.Persistence;
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
    public async Task FragmentEventTypesCarryTheAgentIdInTheirIdSegment()
    {
        var captured = await RunSingleTurnAsync(
            agentId: "alice",
            model: ScriptedLanguageModel.WithText("hi"));

        var fragmentEvents = captured
            .Where(c => c.Data is AgentMessageFragment or AgentThoughtFragment)
            .ToArray();
        var ids = fragmentEvents.Select(c => c.Type.Id).Distinct().ToArray();

        await Assert.That(ids).Count().IsEqualTo(1);
        await Assert.That(ids[0]).IsEqualTo("alice");
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
        var capturing = new CapturingEventPublisher(provider.GetRequiredService<IEventPublisher>());
        var bus = provider.GetRequiredService<IEventBus>();
        var ctx = await provider.GetRequiredService<IContextStore>().OpenAsync(agentId, CancellationToken.None);

        using var captureChannel = new CapturingTurnSubscriber(bus, agentId);
        var seed = new SeedInputChannel([
            new ModelTurn(ModelRole.User, "hello", DateTimeOffset.UtcNow),
        ]);

        using var agent = new global::LlamaShears.Core.Agent(
            id: agentId,
            config: TestAgentConfigs.WithHeartbeat(TimeSpan.Zero),
            model: model,
            agentContext: ctx,
            inputChannels: [seed],
            loggerFactory: NullLoggerFactory.Instance,
            bus: bus,
            systemPromptProvider: new HardcodedSystemPromptProvider(TimeProvider.System),
            timeProvider: new FakeTimeProvider(DateTimeOffset.UnixEpoch),
            compactor: BuildNoOpCompactor(),
            modelConfiguration: new ModelConfiguration("test"),
            agentContextProvider: BuildContextProvider(agentId),
            eventPublisher: capturing);

        await capturing.PublishAsync(
            Event.WellKnown.Host.Tick,
            new SystemTick(DateTimeOffset.UtcNow),
            CancellationToken.None);
        await captureChannel.WaitForTurnAsync(TimeSpan.FromSeconds(5));

        return capturing.Captured;
    }

    private static ServiceProvider BuildServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEventingFramework();
        services.AddSingleton<IContextStore>(new FakeContextStore());
        services.AddEventHandler<AgentTurnContextPersister>();
        var provider = services.BuildServiceProvider();
        provider.GetRequiredService<AgentTurnContextPersister>();
        return provider;
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

    /// <summary>
    /// Records every published event AND forwards to the real bus so the
    /// activated persister still sees turns. Without forwarding, the agent's
    /// agent:turn events would be swallowed and the agent's own context
    /// growth check would never trigger model invocation.
    /// </summary>
    private sealed class CapturingEventPublisher : IEventPublisher
    {
        private readonly IEventPublisher _inner;
        private readonly List<CapturedEvent> _captured = [];
        private readonly Lock _gate = new();

        public CapturingEventPublisher(IEventPublisher inner)
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

        public async ValueTask PublishAsync<T>(EventType eventType, T? data, Guid correlationId, CancellationToken cancellationToken)
            where T : class
        {
            lock (_gate)
            {
                _captured.Add(new CapturedEvent(eventType, data, correlationId));
            }
            await _inner.PublishAsync(eventType, data, correlationId, cancellationToken).ConfigureAwait(false);
        }
    }

    internal sealed record CapturedEvent(EventType Type, object? Data, Guid CorrelationId);
}
