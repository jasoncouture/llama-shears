using LlamaShears.Core;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Persistence;
using LlamaShears.Core.Abstractions.Context;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Channel;
using LlamaShears.Core.Abstractions.Memory;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Core.Eventing;
using LlamaShears.Core.Eventing.Extensions;
using LlamaShears.Core.Abstractions.SystemPrompt;
using LlamaShears.Core.Abstractions.PromptContext;
using LlamaShears.Core.Persistence;
using LlamaShears.Core.Tools.ModelContextProtocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace LlamaShears.UnitTests.Agent.Core;

public sealed class AgentLoopTests
{
    [Test]
    public async Task ChannelMessageDrivesAResponseIntoContextAndOutputs()
    {
        await using var provider = BuildServices();
        var publisher = provider.GetRequiredService<IEventPublisher>();
        var bus = provider.GetRequiredService<IEventBus>();
        var ctx = await provider.GetRequiredService<IContextStore>().OpenAsync("alice", CancellationToken.None);

        using var captured = new CapturingTurnSubscriber(bus, "alice");
        var model = new ScriptedLanguageModel("hi back");

        using var agent = BuildAgent("alice", provider, ctx, model);

        await PublishChannelMessageAsync(publisher, "alice", "hello");

        await captured.WaitForTurnAsync(TimeSpan.FromSeconds(5));

        await Assert.That(captured.Turns).Count().IsEqualTo(1);
        await Assert.That(captured.Turns[0].Role).IsEqualTo(ModelRole.Assistant);
        await Assert.That(captured.Turns[0].Content).IsEqualTo("hi back");
    }

    [Test]
    public async Task ChannelMessageTargetedAtAnotherAgentIsIgnored()
    {
        await using var provider = BuildServices();
        var publisher = provider.GetRequiredService<IEventPublisher>();
        var bus = provider.GetRequiredService<IEventBus>();
        var ctx = await provider.GetRequiredService<IContextStore>().OpenAsync("alice", CancellationToken.None);

        using var captured = new CapturingTurnSubscriber(bus, "alice");
        var model = new ScriptedLanguageModel("should not appear");

        using var agent = BuildAgent("alice", provider, ctx, model);

        await PublishChannelMessageAsync(publisher, "bob", "not for alice");
        await Task.Delay(150, CancellationToken.None);

        await Assert.That(model.PromptInvocations).IsEqualTo(0);
        await Assert.That(captured.Turns).IsEmpty();
    }

    [Test]
    public async Task BroadcastChannelMessageWithNullAgentIdReachesEveryAgent()
    {
        await using var provider = BuildServices();
        var publisher = provider.GetRequiredService<IEventPublisher>();
        var bus = provider.GetRequiredService<IEventBus>();
        var ctx = await provider.GetRequiredService<IContextStore>().OpenAsync("alice", CancellationToken.None);

        using var captured = new CapturingTurnSubscriber(bus, "alice");
        var model = new ScriptedLanguageModel("ack");

        using var agent = BuildAgent("alice", provider, ctx, model);

        await publisher.PublishAsync(
            Event.WellKnown.Channel.Message with { Id = TestChannelId },
            new ChannelMessage("hello everyone", AgentId: null, DateTimeOffset.UtcNow),
            CancellationToken.None);

        await captured.WaitForTurnAsync(TimeSpan.FromSeconds(5));

        await Assert.That(captured.Turns).Count().IsEqualTo(1);
        await Assert.That(captured.Turns[0].Role).IsEqualTo(ModelRole.Assistant);
    }

    private const string TestChannelId = "test";

    private static ValueTask PublishChannelMessageAsync(
        IEventPublisher publisher,
        string agentId,
        string text)
        => publisher.PublishAsync(
            Event.WellKnown.Channel.Message with { Id = TestChannelId },
            new ChannelMessage(text, agentId, DateTimeOffset.UtcNow),
            CancellationToken.None);

    private static global::LlamaShears.Core.Agent BuildAgent(
        string id,
        IServiceProvider services,
        IAgentContext agentContext,
        ILanguageModel model)
    {
        var compactor = Substitute.For<IContextCompactor>();
        compactor.CompactAsync(
                Arg.Any<AgentContext>(),
                Arg.Any<ModelPrompt>(),
                Arg.Any<ILanguageModel>(),
                Arg.Any<ModelConfiguration>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(call => ValueTask.FromResult(call.Arg<ModelPrompt>()));
        var contextProvider = Substitute.For<IAgentContextProvider>();
        contextProvider.CreateAgentContextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<AgentContext?>(TestAgentConfigs.BuildAgentContext(id)));
        var publisher = services.GetRequiredService<IEventPublisher>();
        return new global::LlamaShears.Core.Agent(
            config: TestAgentConfigs.WithHeartbeat(TimeSpan.Zero, id),
            model: model,
            agentContext: agentContext,
            loggerFactory: NullLoggerFactory.Instance,
            bus: services.GetRequiredService<IEventBus>(),
            systemPromptProvider: BuildStubSystemPromptProvider(),
            timeProvider: new FakeTimeProvider(DateTimeOffset.UnixEpoch),
            compactor: compactor,
            modelConfiguration: new ModelConfiguration("test"),
            agentContextProvider: contextProvider,
            eventPublisher: publisher,
            inferenceRunner: new InferenceRunner(publisher, TimeProvider.System),
            toolDispatcher: Substitute.For<IToolCallDispatcher>(),
            currentAgent: Substitute.For<ICurrentAgentAccessor>(),
            promptContext: Substitute.For<IPromptContextProvider>(),
            memorySearcher: Substitute.For<IMemorySearcher>());
    }

    private static ISystemPromptProvider BuildStubSystemPromptProvider()
    {
        var stub = Substitute.For<ISystemPromptProvider>();
        stub.GetAsync(Arg.Any<string?>(), Arg.Any<SystemPromptTemplateParameters>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult("system"));
        return stub;
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
