using LlamaShears.Core.Abstractions.Agent.Persistence;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Context;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Channel;
using LlamaShears.Core.Abstractions.PromptContext;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Core.Abstractions.SystemPrompt;
using LlamaShears.Core.Eventing;
using LlamaShears.Core.Eventing.Extensions;
using LlamaShears.Core.Persistence;
using LlamaShears.Core.Sessions;
using LlamaShears.Core.Tools.ModelContextProtocol;
using LlamaShears.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

using LlamaShears.Core.Abstractions.Common;
namespace LlamaShears.UnitTests.Agent.Core;

public sealed class AgentInterruptTests
{
    private const string TestChannelId = "test";

    [Test]
    public async Task InterruptOnIdleAgentIsNoOp()
    {
        await using var provider = BuildServices();
        var ctx = await provider.GetRequiredService<IContextStore>().OpenAsync("alice", CancellationToken.None);
        using var agent = BuildAgent("alice", provider, ctx, new ScriptedLanguageModel("immediate"));

        await agent.InterruptAsync(CancellationToken.None);
        await agent.InterruptAsync(CancellationToken.None);
    }

    [Test]
    public async Task InterruptCancelsInFlightTurnAndAgentRemainsLive()
    {
        await using var provider = BuildServices();
        var publisher = provider.GetRequiredService<IEventPublisher>();
        var ctx = await provider.GetRequiredService<IContextStore>().OpenAsync("alice", CancellationToken.None);
        var model = new HangingLanguageModel();

        using var agent = BuildAgent("alice", provider, ctx, model);

        await PublishChannelMessageAsync(publisher, "alice", "hang here please");

        await model.WaitForInvocationAsync(TimeSpan.FromSeconds(5));

        await agent.InterruptAsync(CancellationToken.None);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await agent.LockAsync(timeout.Token);
        await agent.UnlockAsync();
    }

    private static ValueTask PublishChannelMessageAsync(
        IEventPublisher publisher,
        string agentId,
        string text)
        => publisher.PublishAsync(
            Event.WellKnown.Channel.Message with { Id = TestChannelId },
            new ChannelMessage(text, agentId, DateTimeOffset.UtcNow),
            CancellationToken.None);

    private static LlamaShears.Core.Agent BuildAgent(
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
        var currentAgent = new CurrentAgentAccessor();
        var resolvedConfig = TestAgentConfigs.WithHeartbeat(TimeSpan.Zero, id);
        var dataContextFactory = TestAgentConfigs.DataContextFactoryWith(resolvedConfig);
        var agentServices = new ServiceCollection();
        agentServices.AddSingleton<IInferenceRunner>(new InferenceRunner(
            publisher,
            Substitute.For<IToolCallDispatcher>(),
            TimeProvider.System,
            Substitute.For<IPromptContextProvider>(),
            TestAgentConfigs.EmptyMemorySearcher(),
            dataContextFactory));
        var agentProvider = agentServices.BuildServiceProvider();
        var agent = new LlamaShears.Core.Agent(
            model: model,
            agentContext: agentContext,
            logger: NullLogger<LlamaShears.Core.Agent>.Instance,
            bus: services.GetRequiredService<IEventBus>(),
            systemPromptProvider: BuildStubSystemPromptProvider(),
            timeProvider: new FakeTimeProvider(DateTimeOffset.UnixEpoch),
            compactor: compactor,
            agentContextProvider: contextProvider,
            eventPublisher: publisher,
            currentAgent: currentAgent,
            dataScope: dataContextFactory.Current!,
            sessionFactory: services.GetRequiredService<ISessionFactory>(),
            scope: agentProvider.CreateAsyncScope());
        agent.Start();
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
