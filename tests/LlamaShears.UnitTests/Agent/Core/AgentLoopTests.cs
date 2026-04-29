using LlamaShears.Agent.Abstractions;
using LlamaShears.Agent.Core;
using LlamaShears.Agent.Core.Channels;
using LlamaShears.Provider.Abstractions;
using MessagePipe;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

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
        var seed = new SeedInputChannel([
            new ModelTurn(ModelRole.User, "hello", DateTimeOffset.UtcNow),
        ]);
        var model = new ScriptedLanguageModel("hi back");

        using var agent = new global::LlamaShears.Agent.Core.Agent(
            id: "alice",
            heartbeatPeriod: TimeSpan.Zero,
            model: model,
            ticks: subscriber,
            seedContext: [],
            inputChannels: [seed],
            outputChannels: [captured],
            logger: NullLogger<global::LlamaShears.Agent.Core.Agent>.Instance);

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

        using var agent = new global::LlamaShears.Agent.Core.Agent(
            id: "alice",
            heartbeatPeriod: TimeSpan.Zero,
            model: model,
            ticks: subscriber,
            seedContext: [],
            inputChannels: [],
            outputChannels: [captured],
            logger: NullLogger<global::LlamaShears.Agent.Core.Agent>.Instance);

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
        var seed = new SeedInputChannel([
            new ModelTurn(ModelRole.User, "hello", DateTimeOffset.UtcNow),
        ]);
        var model = new ScriptedLanguageModel("nope");

        using var agent = new global::LlamaShears.Agent.Core.Agent(
            id: "alice",
            heartbeatPeriod: TimeSpan.Zero,
            model: model,
            ticks: subscriber,
            seedContext: [],
            inputChannels: [seed],
            outputChannels: [captured],
            logger: NullLogger<global::LlamaShears.Agent.Core.Agent>.Instance)
        {
            HeartbeatEnabled = false,
        };

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
        var seed = new SeedInputChannel([
            new ModelTurn(ModelRole.User, "hello", DateTimeOffset.UtcNow),
            new ModelTurn(ModelRole.User, "again", DateTimeOffset.UtcNow.AddSeconds(1)),
        ]);
        var model = new ScriptedLanguageModel("first response");

        using var agent = new global::LlamaShears.Agent.Core.Agent(
            id: "alice",
            heartbeatPeriod: TimeSpan.FromHours(1),
            model: model,
            ticks: subscriber,
            seedContext: [],
            inputChannels: [seed],
            outputChannels: [captured],
            logger: NullLogger<global::LlamaShears.Agent.Core.Agent>.Instance);

        var first = DateTimeOffset.UtcNow;
        await publisher.PublishAsync(new SystemTick(first), CancellationToken.None);
        await captured.WaitForTurnAsync(TimeSpan.FromSeconds(5));
        await publisher.PublishAsync(new SystemTick(first.AddMinutes(1)), CancellationToken.None);
        await Task.Delay(150, CancellationToken.None);

        await Assert.That(model.PromptInvocations).IsEqualTo(1);
    }

    private static ServiceProvider BuildServices()
    {
        var services = new ServiceCollection();
        services.AddMessagePipe();
        return services.BuildServiceProvider();
    }
}
