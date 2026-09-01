using LlamaShears.Core;
using LlamaShears.Core.Abstractions.Agent.Persistence;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Agent;
using LlamaShears.Core.Abstractions.Events.Channel;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Core.Eventing;
using LlamaShears.Core.Eventing.Extensions;
using LlamaShears.Core.Persistence;
using LlamaShears.Core.Sessions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace LlamaShears.UnitTests.Agent.Core;

public sealed class AgentInterruptTests
{
    private const string TestChannelId = "test";

    private readonly AgentLockManager _lockManager = new AgentLockManager();

    [Test]
    public async Task InterruptOnIdleAgentIsNoOp()
    {
        await using var provider = BuildServices();
        var publisher = provider.GetRequiredService<IEventBus>();
        var session = new SessionId("alice", SessionId.DefaultSessionName);
        var ctx = await provider.GetRequiredService<IContextStore>().OpenAsync(session, CancellationToken.None);
        await using var agent = await BuildAgent("alice", session, provider, ctx, new ScriptedLanguageModel("immediate"));

        await PublishInterruptAsync(publisher, session);
        await PublishInterruptAsync(publisher, session);
    }

    [Test]
    public async Task InterruptCancelsInFlightTurnAndAgentRemainsLive()
    {
        await using var provider = BuildServices();
        var publisher = provider.GetRequiredService<IEventBus>();
        var session = new SessionId("alice", SessionId.DefaultSessionName);
        var ctx = await provider.GetRequiredService<IContextStore>().OpenAsync(session, CancellationToken.None);
        var model = new HangingLanguageModel();

        await using var agent = await BuildAgent("alice", session, provider, ctx, model);

        await PublishChannelMessageAsync(publisher, session, "hang here please");

        await model.WaitForInvocationAsync(TimeSpan.FromMilliseconds(500));

        await PublishInterruptAsync(publisher, session);

        using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
        using var idle = await _lockManager.AcquireLockAsync("alice", timeout.Token);
    }

    private static ValueTask PublishInterruptAsync(IEventBus publisher, SessionId session)
        => publisher.PublishAsync(
            Event.WellKnown.Command.InterruptAgent with { Id = session },
            AgentInterruptRequest.Instance,
            CancellationToken.None);

    private static ValueTask PublishChannelMessageAsync(
        IEventBus publisher,
        SessionId session,
        string text)
        => publisher.PublishAsync(
            Event.WellKnown.Channel.Message with { Id = session },
            new ChannelMessage(text, TestChannelId, DateTimeOffset.UtcNow),
            CancellationToken.None);

    private Task<LlamaShears.Core.Agent> BuildAgent(
        string id,
        SessionId session,
        IServiceProvider services,
        IAgentContext agentContext,
        ILanguageModel model)
        => AgentHarness.StartAsync(
            id,
            session,
            services,
            agentContext,
            model,
            lockManager: _lockManager);

    private static ServiceProvider BuildServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEventingFramework();
        services.AddSingleton<IContextStore>(new FakeContextStore());
        services.AddSingleton(Substitute.For<IDataContextFactory>());
        services.AddEventHandler<AgentTurnContextPersister>();
        services.AddSingleton<ISessionFactory, SessionFactory>();
        var provider = services.BuildServiceProvider();
        provider.GetRequiredService<AgentTurnContextPersister>();
        return provider;
    }
}
