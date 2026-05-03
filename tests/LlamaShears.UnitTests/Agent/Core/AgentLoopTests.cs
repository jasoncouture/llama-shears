using LlamaShears.Core;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Events;
using LlamaShears.Core.Abstractions.Agent.Persistence;
using LlamaShears.Core.Abstractions.Context;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Provider;
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

public sealed class AgentLoopTests
{
    [Test]
    public async Task TickWithPendingInputDrivesAResponseIntoContextAndOutputs()
    {
        await using var provider = BuildServices();
        var publisher = provider.GetRequiredService<IEventPublisher>();
        var bus = provider.GetRequiredService<IEventBus>();
        var ctx = await provider.GetRequiredService<IContextStore>().OpenAsync("alice", CancellationToken.None);

        using var captured = new CapturingTurnSubscriber(bus, "alice");
        var seed = new global::LlamaShears.Core.Channels.SeedInputChannel([
            new ModelTurn(ModelRole.User, "hello", DateTimeOffset.UtcNow),
        ]);
        var model = new ScriptedLanguageModel("hi back");

        using var agent = BuildAgent("alice", provider, ctx, model, [seed]);

        await PublishTickAsync(publisher, DateTimeOffset.UtcNow);

        await captured.WaitForTurnAsync(TimeSpan.FromSeconds(5));

        await Assert.That(captured.Turns).Count().IsEqualTo(1);
        await Assert.That(captured.Turns[0].Role).IsEqualTo(ModelRole.Assistant);
        await Assert.That(captured.Turns[0].Content).IsEqualTo("hi back");
    }

    [Test]
    public async Task TickWithNoInputDoesNotInvokeTheModel()
    {
        await using var provider = BuildServices();
        var publisher = provider.GetRequiredService<IEventPublisher>();
        var bus = provider.GetRequiredService<IEventBus>();
        var ctx = await provider.GetRequiredService<IContextStore>().OpenAsync("alice", CancellationToken.None);

        using var captured = new CapturingTurnSubscriber(bus, "alice");
        var model = new ScriptedLanguageModel("should not appear");

        using var agent = BuildAgent("alice", provider, ctx, model, []);

        await PublishTickAsync(publisher, DateTimeOffset.UtcNow);
        await Task.Delay(150, CancellationToken.None);

        await Assert.That(model.PromptInvocations).IsEqualTo(0);
        await Assert.That(captured.Turns).IsEmpty();
    }

    [Test]
    public async Task HeartbeatPeriodThrottlesSubsequentTicksWithinWindow()
    {
        await using var provider = BuildServices();
        var publisher = provider.GetRequiredService<IEventPublisher>();
        var bus = provider.GetRequiredService<IEventBus>();
        var ctx = await provider.GetRequiredService<IContextStore>().OpenAsync("alice", CancellationToken.None);

        using var captured = new CapturingTurnSubscriber(bus, "alice");
        var seed = new global::LlamaShears.Core.Channels.SeedInputChannel([
            new ModelTurn(ModelRole.User, "hello", DateTimeOffset.UtcNow),
            new ModelTurn(ModelRole.User, "again", DateTimeOffset.UtcNow.AddSeconds(1)),
        ]);
        var model = new ScriptedLanguageModel("first response");

        using var agent = BuildAgent(
            "alice",
            provider,
            ctx,
            model,
            [seed],
            heartbeatPeriod: TimeSpan.FromHours(1));

        var first = DateTimeOffset.UtcNow;
        await PublishTickAsync(publisher, first);
        await captured.WaitForTurnAsync(TimeSpan.FromSeconds(5));
        await PublishTickAsync(publisher, first.AddMinutes(1));
        await Task.Delay(150, CancellationToken.None);

        await Assert.That(model.PromptInvocations).IsEqualTo(1);
    }

    private static ValueTask PublishTickAsync(IEventPublisher publisher, DateTimeOffset at)
        => publisher.PublishAsync(
            Event.WellKnown.Host.Tick,
            new SystemTick(at),
            CancellationToken.None);

    private static global::LlamaShears.Core.Agent BuildAgent(
        string id,
        IServiceProvider services,
        IAgentContext agentContext,
        ILanguageModel model,
        IReadOnlyList<IInputChannel> inputs,
        TimeSpan? heartbeatPeriod = null)
    {
        var compactor = Substitute.For<IContextCompactor>();
        compactor.CompactAsync(
                Arg.Any<AgentContext>(),
                Arg.Any<ModelPrompt>(),
                Arg.Any<ILanguageModel>(),
                Arg.Any<ModelConfiguration>(),
                Arg.Any<CancellationToken>())
            .Returns(call => ValueTask.FromResult(call.Arg<ModelPrompt>()));
        var contextProvider = Substitute.For<IAgentContextProvider>();
        contextProvider.CreateAgentContextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<AgentContext?>(TestAgentConfigs.BuildAgentContext(id)));
        return new global::LlamaShears.Core.Agent(
            id: id,
            config: TestAgentConfigs.WithHeartbeat(heartbeatPeriod ?? TimeSpan.Zero),
            model: model,
            agentContext: agentContext,
            inputChannels: inputs,
            loggerFactory: NullLoggerFactory.Instance,
            bus: services.GetRequiredService<IEventBus>(),
            systemPromptProvider: new HardcodedSystemPromptProvider(TimeProvider.System),
            timeProvider: new FakeTimeProvider(DateTimeOffset.UnixEpoch),
            compactor: compactor,
            modelConfiguration: new ModelConfiguration("test"),
            agentContextProvider: contextProvider,
            eventPublisher: services.GetRequiredService<IEventPublisher>());
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
}
