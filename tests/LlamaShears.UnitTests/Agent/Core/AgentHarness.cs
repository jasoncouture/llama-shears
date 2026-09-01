using LlamaShears.Core;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Persistence;
using LlamaShears.Core.Abstractions.Agent.Pipeline;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.Context;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Memory;
using LlamaShears.Core.Abstractions.PromptContext;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Core.Abstractions.SystemPrompt;
using LlamaShears.Core.Pipeline;
using LlamaShears.Core.Tools.ModelContextProtocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace LlamaShears.UnitTests.Agent.Core;

internal static class AgentHarness
{
    public static async Task<LlamaShears.Core.Agent> StartAsync(
        string id,
        SessionId session,
        IServiceProvider services,
        IAgentContext agentContext,
        ILanguageModel model,
        IAgentLockManager? lockManager = null,
        IToolCallDispatcher? dispatcher = null,
        IMemorySearcher? memorySearcher = null,
        AgentConfig? config = null,
        IEventBus? eventBus = null)
    {
        var bus = eventBus ?? services.GetRequiredService<IEventBus>();
        var resolvedConfig = config ?? TestAgentConfigs.WithHeartbeat(TimeSpan.Zero, id);
        var dataContextFactory = TestAgentConfigs.DataContextFactoryWith(resolvedConfig, session);
        var dataScope = dataContextFactory.Current!;
        services.GetRequiredService<IDataContextFactory>().Current = dataScope;

        var compactor = Substitute.For<IContextCompactor>();
        compactor.CompactAsync(
                Arg.Any<AgentContext>(),
                Arg.Any<ModelPrompt>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(call => ValueTask.FromResult(call.Arg<ModelPrompt>()));
        var contextProvider = Substitute.For<IAgentContextProvider>();
        contextProvider.CreateAgentContextAsync(Arg.Any<SessionId>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<AgentContext?>(TestAgentConfigs.BuildAgentContext(id)));

        var iterationServices = new ServiceCollection();
        iterationServices.AddSingleton<IDataContextScope>(dataScope);
        iterationServices.AddSingleton(compactor);
        iterationServices.AddSingleton(model);
        iterationServices.AddSingleton(TestAgentConfigs.BuildEmptyServerRegistry());
        iterationServices.AddSingleton(TestAgentConfigs.BuildEmptyToolDiscovery());
        iterationServices.AddSingleton<IAgentStateTracker>(new AgentStateTracker(dataScope));
        iterationServices.AddMemoryCache();
        iterationServices.AddSingleton<IInferenceRunner>(new InferenceRunner(
            bus,
            dispatcher ?? Substitute.For<IToolCallDispatcher>(),
            TimeProvider.System,
            Substitute.For<IPromptContextProvider>(),
            BuildStubSystemPromptProvider(),
            memorySearcher ?? TestAgentConfigs.EmptyMemorySearcher(),
            dataScope,
            model,
            NullLogger<InferenceRunner>.Instance));
        var iterationProvider = iterationServices.BuildServiceProvider();
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UnixEpoch);
        var iterationRunner = new AgentIterationRunner(
            NullLogger<AgentIterationRunner>.Instance,
            timeProvider,
            bus,
            dataScope,
            iterationProvider.GetRequiredService<IServiceScopeFactory>(),
            contextProvider);

        var sessionFactory = services.GetRequiredService<ISessionFactory>();
        var sessionQueue = sessionFactory.Get(session);
        var lifetime = new AgentLifetime();
        var activeTurn = new ActiveTurnCancellation();
        var agentLock = new AgentLock(lockManager ?? new AgentLockManager(), dataScope);
        IAgentPipeline pipeline = new AgentPipeline(
        [
            new TurnExceptionMiddleware(NullLogger<TurnExceptionMiddleware>.Instance, dataScope),
            new AgentActivityMiddleware(dataScope),
            new CorrelationScopeMiddleware(NullLogger<CorrelationScopeMiddleware>.Instance),
            new AgentLockMiddleware(agentLock),
            new InterruptScopeMiddleware(activeTurn),
            new ToolResultEnqueueMiddleware(sessionQueue),
            new RunIterationMiddleware(iterationRunner),
        ]);

        IAgentService[] inbound =
        [
            new ChannelMessageIntakeService(bus, sessionQueue, dataScope),
            new AgentInterruptService(bus, dataScope, activeTurn),
            new AgentShutdownService(bus, dataScope, lifetime),
            new AgentConfigReloadService(bus, dataScope, NullLogger<AgentConfigReloadService>.Instance),
        ];

        var agent = new LlamaShears.Core.Agent(
            contextStore: new FakeContextStore().With(session, agentContext),
            logger: NullLogger<LlamaShears.Core.Agent>.Instance,
            eventBus: bus,
            timeProvider: timeProvider,
            dataScope: dataScope,
            sessionFactory: sessionFactory,
            pipeline: pipeline,
            lifetime: lifetime,
            agentServices: inbound);
        await AgentStartHelper.StartAndWaitAsync(bus, session, agent);
        return agent;
    }

    private static ISystemPromptProvider BuildStubSystemPromptProvider()
    {
        var stub = Substitute.For<ISystemPromptProvider>();
        stub.GetAsync(Arg.Any<string?>(), Arg.Any<IReadOnlyDictionary<string, object?>>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult("system"));
        return stub;
    }
}
