using LlamaShears.Agent.Abstractions;
using LlamaShears.Agent.Core;
using LlamaShears.Agent.Core.SystemPrompt;
using LlamaShears.Provider.Abstractions;
using MessagePipe;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace LlamaShears.UnitTests.Agent.Core;

public sealed class AgentLoopTests
{
    [Test]
    public async Task TickWithPendingInputDrivesAResponseIntoContextAndOutputs()
    {
        await using var provider = BuildServices();
        var publisher = provider.GetRequiredService<IAsyncPublisher<SystemTick>>();
        var subscriber = provider.GetRequiredService<IAsyncSubscriber<SystemTick>>();

        var captured = new CapturingOutputChannel();
        var seed = new global::LlamaShears.Agent.Core.Channels.SeedInputChannel([
            new ModelTurn(ModelRole.User, "hello", DateTimeOffset.UtcNow),
        ]);
        var model = new ScriptedLanguageModel("hi back");

        using var agent = BuildAgent("alice", subscriber, model, [seed], [captured]);

        await publisher.PublishAsync(new SystemTick(DateTimeOffset.UtcNow), CancellationToken.None);

        await captured.WaitForTurnAsync(TimeSpan.FromSeconds(5));

        await Assert.That(captured.Turns).Count().IsEqualTo(1);
        await Assert.That(captured.Turns[0].Role).IsEqualTo(ModelRole.Assistant);
        await Assert.That(captured.Turns[0].Content).IsEqualTo("hi back");
    }

    [Test]
    public async Task TickWithNoInputDoesNotInvokeTheModel()
    {
        await using var provider = BuildServices();
        var publisher = provider.GetRequiredService<IAsyncPublisher<SystemTick>>();
        var subscriber = provider.GetRequiredService<IAsyncSubscriber<SystemTick>>();

        var captured = new CapturingOutputChannel();
        var model = new ScriptedLanguageModel("should not appear");

        using var agent = BuildAgent("alice", subscriber, model, [], [captured]);

        await publisher.PublishAsync(new SystemTick(DateTimeOffset.UtcNow), CancellationToken.None);
        await Task.Delay(150, CancellationToken.None);

        await Assert.That(model.PromptInvocations).IsEqualTo(0);
        await Assert.That(captured.Turns).IsEmpty();
    }

    [Test]
    public async Task DisabledHeartbeatSwallowsTicks()
    {
        await using var provider = BuildServices();
        var publisher = provider.GetRequiredService<IAsyncPublisher<SystemTick>>();
        var subscriber = provider.GetRequiredService<IAsyncSubscriber<SystemTick>>();

        var captured = new CapturingOutputChannel();
        var seed = new global::LlamaShears.Agent.Core.Channels.SeedInputChannel([
            new ModelTurn(ModelRole.User, "hello", DateTimeOffset.UtcNow),
        ]);
        var model = new ScriptedLanguageModel("nope");

        using var agent = BuildAgent("alice", subscriber, model, [seed], [captured]);
        agent.HeartbeatEnabled = false;

        await publisher.PublishAsync(new SystemTick(DateTimeOffset.UtcNow), CancellationToken.None);
        await Task.Delay(150, CancellationToken.None);

        await Assert.That(model.PromptInvocations).IsEqualTo(0);
        await Assert.That(captured.Turns).IsEmpty();
    }

    [Test]
    public async Task HeartbeatPeriodThrottlesSubsequentTicksWithinWindow()
    {
        await using var provider = BuildServices();
        var publisher = provider.GetRequiredService<IAsyncPublisher<SystemTick>>();
        var subscriber = provider.GetRequiredService<IAsyncSubscriber<SystemTick>>();

        var captured = new CapturingOutputChannel();
        var seed = new global::LlamaShears.Agent.Core.Channels.SeedInputChannel([
            new ModelTurn(ModelRole.User, "hello", DateTimeOffset.UtcNow),
            new ModelTurn(ModelRole.User, "again", DateTimeOffset.UtcNow.AddSeconds(1)),
        ]);
        var model = new ScriptedLanguageModel("first response");

        using var agent = BuildAgent(
            "alice",
            subscriber,
            model,
            [seed],
            [captured],
            heartbeatPeriod: TimeSpan.FromHours(1));

        var first = DateTimeOffset.UtcNow;
        await publisher.PublishAsync(new SystemTick(first), CancellationToken.None);
        await captured.WaitForTurnAsync(TimeSpan.FromSeconds(5));
        await publisher.PublishAsync(new SystemTick(first.AddMinutes(1)), CancellationToken.None);
        await Task.Delay(150, CancellationToken.None);

        await Assert.That(model.PromptInvocations).IsEqualTo(1);
    }

    [Test]
    public async Task ConfigPropertyExposesTheFullAgentConfig()
    {
        await using var provider = BuildServices();
        var subscriber = provider.GetRequiredService<IAsyncSubscriber<SystemTick>>();

        var config = TestAgentConfigs.WithHeartbeat(TimeSpan.FromMinutes(15));
        using var agent = new global::LlamaShears.Agent.Core.Agent(
            id: "alice",
            config: config,
            model: new ScriptedLanguageModel("ignored"),
            agentContext: new FakeAgentContext("alice"),
            inputChannels: [],
            outputChannels: [],
            logger: NullLogger.Instance,
            ticks: subscriber,
            systemPromptProvider: new HardcodedSystemPromptProvider(),
            timeProvider: new FakeTimeProvider(DateTimeOffset.UnixEpoch));

        await Assert.That(agent.Config).IsSameReferenceAs(config);
        await Assert.That(agent.HeartbeatPeriod).IsEqualTo(TimeSpan.FromMinutes(15));
    }

    private static global::LlamaShears.Agent.Core.Agent BuildAgent(
        string id,
        IAsyncSubscriber<SystemTick> ticks,
        ILanguageModel model,
        IReadOnlyList<IInputChannel> inputs,
        IReadOnlyList<IOutputChannel> outputs,
        TimeSpan? heartbeatPeriod = null)
    {
        return new global::LlamaShears.Agent.Core.Agent(
            id: id,
            config: TestAgentConfigs.WithHeartbeat(heartbeatPeriod ?? TimeSpan.Zero),
            model: model,
            agentContext: new FakeAgentContext(id),
            inputChannels: inputs,
            outputChannels: outputs,
            logger: NullLogger.Instance,
            ticks: ticks,
            systemPromptProvider: new HardcodedSystemPromptProvider(),
            timeProvider: new FakeTimeProvider(DateTimeOffset.UnixEpoch));
    }

    private static ServiceProvider BuildServices()
    {
        var services = new ServiceCollection();
        services.AddMessagePipe();
        return services.BuildServiceProvider();
    }
}
