using LlamaShears.Core;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Persistence;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Sessions;
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

using LlamaShears.Core.Abstractions.Common;
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

        await using var agent = await BuildAgent("alice", provider, ctx, model);

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

        await using var agent = await BuildAgent("alice", provider, ctx, model);

        await PublishChannelMessageAsync(publisher, "bob", "not for alice");

        await Assert.That(model.PromptInvocations).IsEqualTo(0);
        await Assert.That(captured.Turns).IsEmpty();
    }

    [Test]
    public async Task UserMessageInvokesMemorySearcherOnceAndStillProducesTurn()
    {
        await using var provider = BuildServices();
        var publisher = provider.GetRequiredService<IEventPublisher>();
        var bus = provider.GetRequiredService<IEventBus>();
        var ctx = await provider.GetRequiredService<IContextStore>().OpenAsync("alice", CancellationToken.None);

        using var captured = new CapturingTurnSubscriber(bus, "alice");
        var model = new ScriptedLanguageModel("hi back");
        var memorySearcher = Substitute.For<IMemorySearcher>();
        memorySearcher
            .SearchAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<double?>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<IReadOnlyList<MemorySearchResult>>([]));

        var config = TestAgentConfigs.WithHeartbeat(TimeSpan.Zero, "alice") with
        {
            WorkspacePath = Path.Combine(Path.GetTempPath(), $"memory-search-{Guid.NewGuid():N}"),
        };

        await using var agent = await BuildAgent("alice", provider, ctx, model, config, memorySearcher);

        await PublishChannelMessageAsync(publisher, "alice", "hello");

        await captured.WaitForTurnAsync(TimeSpan.FromSeconds(5));

        await Assert.That(captured.Turns).Count().IsEqualTo(1);
        await Assert.That(captured.Turns[0].Role).IsEqualTo(ModelRole.Assistant);
        await memorySearcher
            .Received(1)
            .SearchAsync(
                "alice",
                Arg.Any<string>(),
                Arg.Any<int?>(),
                Arg.Any<double?>(),
                Arg.Any<CancellationToken>());
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

        await using var agent = await BuildAgent("alice", provider, ctx, model);

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

    private static async Task<LlamaShears.Core.Agent> BuildAgent(
        string id,
        IServiceProvider services,
        IAgentContext agentContext,
        ILanguageModel model,
        AgentConfig? config = null,
        IMemorySearcher? memorySearcher = null)
    {
        var compactor = Substitute.For<IContextCompactor>();
        compactor.CompactAsync(
                Arg.Any<AgentContext>(),
                Arg.Any<ModelPrompt>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(call => ValueTask.FromResult(call.Arg<ModelPrompt>()));
        var contextProvider = Substitute.For<IAgentContextProvider>();
        contextProvider.CreateAgentContextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<AgentContext?>(TestAgentConfigs.BuildAgentContext(id)));
        var publisher = services.GetRequiredService<IEventPublisher>();
        var resolvedMemorySearcher = memorySearcher ?? TestAgentConfigs.EmptyMemorySearcher();
        var resolvedConfig = config ?? TestAgentConfigs.WithHeartbeat(TimeSpan.Zero, id);
        var dataContextFactory = TestAgentConfigs.DataContextFactoryWith(resolvedConfig);
        var agentServices = new ServiceCollection();
        agentServices.AddSingleton(dataContextFactory.Current!);
        agentServices.AddSingleton<IContextCompactor>(compactor);
        agentServices.AddSingleton<ILanguageModel>(model);
        agentServices.AddSingleton<IModelContextProtocolServerRegistry>(TestAgentConfigs.BuildEmptyServerRegistry());
        agentServices.AddSingleton<IModelContextProtocolToolDiscovery>(TestAgentConfigs.BuildEmptyToolDiscovery());
        agentServices.AddSingleton<IAgentStateTracker>(new AgentStateTracker(dataContextFactory.Current!));
        agentServices.AddMemoryCache();
        agentServices.AddSingleton<IInferenceRunner>(new InferenceRunner(
            publisher,
            Substitute.For<IToolCallDispatcher>(),
            TimeProvider.System,
            Substitute.For<IPromptContextProvider>(),
            BuildStubSystemPromptProvider(),
            resolvedMemorySearcher,
            dataContextFactory.Current!,
            model,
            NullLogger<InferenceRunner>.Instance));
        var agentProvider = agentServices.BuildServiceProvider();
        var contextStore = new FakeContextStore().With(id, agentContext);
        var agent = new LlamaShears.Core.Agent(
            contextStore: contextStore,
            logger: NullLogger<LlamaShears.Core.Agent>.Instance,
            bus: services.GetRequiredService<IEventBus>(),
            timeProvider: new FakeTimeProvider(DateTimeOffset.UnixEpoch),
            agentContextProvider: contextProvider,
            eventPublisher: publisher,
            dataScope: dataContextFactory.Current!,
            agentLock: new AgentLock(new AgentLockManager(), dataContextFactory.Current!),
            sessionFactory: services.GetRequiredService<ISessionFactory>(),
            scopeFactory: agentProvider.GetRequiredService<IServiceScopeFactory>());
        await agent.StartAsync(CancellationToken.None);
        return agent;
    }

    private static ISystemPromptProvider BuildStubSystemPromptProvider()
    {
        var stub = Substitute.For<ISystemPromptProvider>();
        stub.GetAsync(Arg.Any<string?>(), Arg.Any<IReadOnlyDictionary<string, object?>>(), Arg.Any<CancellationToken>())
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
        services.AddSingleton<ISessionFactory, SessionFactory>();
        var provider = services.BuildServiceProvider();
        provider.GetRequiredService<AgentTurnContextPersister>();
        return provider;
    }
}
