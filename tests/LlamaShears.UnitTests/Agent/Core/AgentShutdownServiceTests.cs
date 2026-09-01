using LlamaShears.Core;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Pipeline;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Agent;
using LlamaShears.UnitTests.Agent.Pipeline;
using NSubstitute;

namespace LlamaShears.UnitTests.Agent.Core;

public sealed class AgentShutdownServiceTests
{
    [Test]
    public async Task HandleAsyncStopsWhenSessionMatches()
    {
        var lifetime = Substitute.For<IAgentLifetime>();
        lifetime.Stopping.Returns(CancellationToken.None);
        var scope = PipelineTestContext.ScopeFor();
        IAgentService service = new AgentShutdownService(
            Substitute.For<IEventBus>(),
            scope,
            lifetime);
        var envelope = Substitute.For<IEventEnvelope<AgentShutdownRequest>>();
        envelope.Data.Returns(new AgentShutdownRequest(scope.Key));

        await ((IEventHandler<AgentShutdownRequest>)service).HandleAsync(envelope, CancellationToken.None);

        lifetime.Received(1).Stop();
    }

    [Test]
    public async Task HandleAsyncIgnoresADifferentSession()
    {
        var lifetime = Substitute.For<IAgentLifetime>();
        lifetime.Stopping.Returns(CancellationToken.None);
        IAgentService service = new AgentShutdownService(
            Substitute.For<IEventBus>(),
            PipelineTestContext.ScopeFor("alice"),
            lifetime);
        var envelope = Substitute.For<IEventEnvelope<AgentShutdownRequest>>();
        envelope.Data.Returns(new AgentShutdownRequest(new SessionId("bob", SessionId.DefaultSessionName)));

        await ((IEventHandler<AgentShutdownRequest>)service).HandleAsync(envelope, CancellationToken.None);

        lifetime.DidNotReceive().Stop();
    }

    [Test]
    public async Task HandleAsyncStopsOnBroadcastWithNullSession()
    {
        var lifetime = Substitute.For<IAgentLifetime>();
        lifetime.Stopping.Returns(CancellationToken.None);
        IAgentService service = new AgentShutdownService(
            Substitute.For<IEventBus>(),
            PipelineTestContext.ScopeFor(),
            lifetime);
        var envelope = Substitute.For<IEventEnvelope<AgentShutdownRequest>>();
        envelope.Data.Returns(new AgentShutdownRequest(null));

        await ((IEventHandler<AgentShutdownRequest>)service).HandleAsync(envelope, CancellationToken.None);

        lifetime.Received(1).Stop();
    }

    [Test]
    public async Task HandleAsyncIsANoOpWhenAlreadyStopping()
    {
        using var stopping = new CancellationTokenSource();
        stopping.Cancel();
        var lifetime = Substitute.For<IAgentLifetime>();
        lifetime.Stopping.Returns(stopping.Token);
        IAgentService service = new AgentShutdownService(
            Substitute.For<IEventBus>(),
            PipelineTestContext.ScopeFor(),
            lifetime);
        var envelope = Substitute.For<IEventEnvelope<AgentShutdownRequest>>();
        envelope.Data.Returns(new AgentShutdownRequest(null));

        await ((IEventHandler<AgentShutdownRequest>)service).HandleAsync(envelope, CancellationToken.None);

        lifetime.DidNotReceive().Stop();
    }
}
