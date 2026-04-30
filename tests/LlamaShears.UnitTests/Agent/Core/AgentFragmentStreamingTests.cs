using LlamaShears.Agent.Core.Channels;
using LlamaShears.Agent.Core.SystemPrompt;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Events;
using LlamaShears.Core.Abstractions.Provider;
using MessagePipe;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace LlamaShears.UnitTests.Agent.Core;

public sealed class AgentFragmentStreamingTests
{
    [Test]
    public async Task TextFragmentsArePublishedPerChunkWithAFinalMarker()
    {
        await using var provider = BuildServices();
        var tickPublisher = provider.GetRequiredService<IAsyncPublisher<SystemTick>>();
        var ticks = provider.GetRequiredService<IAsyncSubscriber<SystemTick>>();
        var fragmentPublisher = provider.GetRequiredService<IAsyncPublisher<AgentFragmentEmitted>>();
        var fragmentSubscriber = provider.GetRequiredService<IAsyncSubscriber<AgentFragmentEmitted>>();

        var captured = new List<AgentFragmentEmitted>();
        var captureGate = new Lock();
        using var subscription = fragmentSubscriber.Subscribe((evt, _) =>
        {
            lock (captureGate)
            {
                captured.Add(evt);
            }
            return ValueTask.CompletedTask;
        });

        var captureChannel = new CapturingOutputChannel();
        var seed = new SeedInputChannel([
            new ModelTurn(ModelRole.User, "hello", DateTimeOffset.UtcNow),
        ]);
        var model = ScriptedLanguageModel.WithText("Hi", " there", "!");

        using var agent = new global::LlamaShears.Agent.Core.Agent(
            id: "alice",
            config: TestAgentConfigs.WithHeartbeat(TimeSpan.Zero),
            model: model,
            agentContext: new FakeAgentContext("alice"),
            inputChannels: [seed],
            outputChannels: [captureChannel],
            logger: NullLogger.Instance,
            ticks: ticks,
            systemPromptProvider: new HardcodedSystemPromptProvider(),
            timeProvider: new FakeTimeProvider(DateTimeOffset.UnixEpoch),
            fragments: fragmentPublisher);

        await tickPublisher.PublishAsync(new SystemTick(DateTimeOffset.UtcNow), CancellationToken.None);
        await captureChannel.WaitForTurnAsync(TimeSpan.FromSeconds(5));

        IReadOnlyList<AgentFragmentEmitted> snapshot;
        lock (captureGate)
        {
            snapshot = [..captured];
        }

        var textFragments = snapshot.Where(f => f.Kind == AgentFragmentKind.Text).ToArray();
        await Assert.That(textFragments).Count().IsEqualTo(4);
        await Assert.That(textFragments[..3].Select(f => f.IsFinal).ToArray())
            .IsEquivalentTo(new[] { false, false, false });
        await Assert.That(textFragments[3].IsFinal).IsTrue();
        await Assert.That(textFragments.Select(f => f.Delta).ToArray())
            .IsEquivalentTo(new[] { "Hi", " there", "!", string.Empty });
        await Assert.That(textFragments.Select(f => f.StreamId).Distinct().Count()).IsEqualTo(1);
        await Assert.That(textFragments[0].AgentId).IsEqualTo("alice");
    }

    [Test]
    public async Task ThoughtAndTextStreamsUseDistinctStreamIds()
    {
        await using var provider = BuildServices();
        var tickPublisher = provider.GetRequiredService<IAsyncPublisher<SystemTick>>();
        var ticks = provider.GetRequiredService<IAsyncSubscriber<SystemTick>>();
        var fragmentPublisher = provider.GetRequiredService<IAsyncPublisher<AgentFragmentEmitted>>();
        var fragmentSubscriber = provider.GetRequiredService<IAsyncSubscriber<AgentFragmentEmitted>>();

        var captured = new List<AgentFragmentEmitted>();
        var captureGate = new Lock();
        using var subscription = fragmentSubscriber.Subscribe((evt, _) =>
        {
            lock (captureGate)
            {
                captured.Add(evt);
            }
            return ValueTask.CompletedTask;
        });

        var captureChannel = new CapturingOutputChannel();
        var seed = new SeedInputChannel([
            new ModelTurn(ModelRole.User, "hello", DateTimeOffset.UtcNow),
        ]);
        var model = ScriptedLanguageModel.WithThoughtThenText(["thinking..."], ["done"]);

        using var agent = new global::LlamaShears.Agent.Core.Agent(
            id: "alice",
            config: TestAgentConfigs.WithHeartbeat(TimeSpan.Zero),
            model: model,
            agentContext: new FakeAgentContext("alice"),
            inputChannels: [seed],
            outputChannels: [captureChannel],
            logger: NullLogger.Instance,
            ticks: ticks,
            systemPromptProvider: new HardcodedSystemPromptProvider(),
            timeProvider: new FakeTimeProvider(DateTimeOffset.UnixEpoch),
            fragments: fragmentPublisher);

        await tickPublisher.PublishAsync(new SystemTick(DateTimeOffset.UtcNow), CancellationToken.None);
        await captureChannel.WaitForTurnAsync(TimeSpan.FromSeconds(5));

        IReadOnlyList<AgentFragmentEmitted> snapshot;
        lock (captureGate)
        {
            snapshot = [..captured];
        }

        var thoughtIds = snapshot.Where(f => f.Kind == AgentFragmentKind.Thought).Select(f => f.StreamId).Distinct().ToArray();
        var textIds = snapshot.Where(f => f.Kind == AgentFragmentKind.Text).Select(f => f.StreamId).Distinct().ToArray();
        await Assert.That(thoughtIds).Count().IsEqualTo(1);
        await Assert.That(textIds).Count().IsEqualTo(1);
        await Assert.That(thoughtIds[0]).IsNotEqualTo(textIds[0]);
    }

    [Test]
    public async Task StreamsThatProducedNoFragmentsEmitNoFinalMarker()
    {
        await using var provider = BuildServices();
        var tickPublisher = provider.GetRequiredService<IAsyncPublisher<SystemTick>>();
        var ticks = provider.GetRequiredService<IAsyncSubscriber<SystemTick>>();
        var fragmentPublisher = provider.GetRequiredService<IAsyncPublisher<AgentFragmentEmitted>>();
        var fragmentSubscriber = provider.GetRequiredService<IAsyncSubscriber<AgentFragmentEmitted>>();

        var captured = new List<AgentFragmentEmitted>();
        var captureGate = new Lock();
        using var subscription = fragmentSubscriber.Subscribe((evt, _) =>
        {
            lock (captureGate)
            {
                captured.Add(evt);
            }
            return ValueTask.CompletedTask;
        });

        var captureChannel = new CapturingOutputChannel();
        var seed = new SeedInputChannel([
            new ModelTurn(ModelRole.User, "hello", DateTimeOffset.UtcNow),
        ]);
        // Text only — no thought stream should emit a final.
        var model = ScriptedLanguageModel.WithText("hi");

        using var agent = new global::LlamaShears.Agent.Core.Agent(
            id: "alice",
            config: TestAgentConfigs.WithHeartbeat(TimeSpan.Zero),
            model: model,
            agentContext: new FakeAgentContext("alice"),
            inputChannels: [seed],
            outputChannels: [captureChannel],
            logger: NullLogger.Instance,
            ticks: ticks,
            systemPromptProvider: new HardcodedSystemPromptProvider(),
            timeProvider: new FakeTimeProvider(DateTimeOffset.UnixEpoch),
            fragments: fragmentPublisher);

        await tickPublisher.PublishAsync(new SystemTick(DateTimeOffset.UtcNow), CancellationToken.None);
        await captureChannel.WaitForTurnAsync(TimeSpan.FromSeconds(5));

        IReadOnlyList<AgentFragmentEmitted> snapshot;
        lock (captureGate)
        {
            snapshot = [..captured];
        }

        await Assert.That(snapshot.Any(f => f.Kind == AgentFragmentKind.Thought)).IsFalse();
    }

    private static ServiceProvider BuildServices()
    {
        var services = new ServiceCollection();
        services.AddMessagePipe();
        return services.BuildServiceProvider();
    }
}
