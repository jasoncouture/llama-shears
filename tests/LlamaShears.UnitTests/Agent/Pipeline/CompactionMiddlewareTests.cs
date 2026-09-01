using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Pipeline;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Context;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Core.Pipeline;
using LlamaShears.UnitTests.Agent.Core;
using NSubstitute;

namespace LlamaShears.UnitTests.Agent.Pipeline;

public sealed class CompactionMiddlewareTests
{
    [Test]
    public async Task CompactsPersistedTurnsAfterNextCompletes()
    {
        var nextCalled = false;
        var compactAfterNext = false;
        var remembered = new ModelTurn(ModelRole.User, "remembered", DateTimeOffset.UnixEpoch);
        var snapshot = TestAgentConfigs.BuildAgentContext("alice");
        var provider = Substitute.For<IAgentContextProvider>();
        provider
            .CreateAgentContextAsync(Arg.Any<SessionId>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<AgentContext?>(snapshot));
        var compactor = Substitute.For<IContextCompactor>();
        ModelPrompt? sent = null;
        compactor
            .CompactAsync(
                Arg.Any<AgentContext>(),
                Arg.Any<ModelPrompt>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                compactAfterNext = nextCalled;
                sent = call.Arg<ModelPrompt>();
                return ValueTask.FromResult(call.Arg<ModelPrompt>());
            });
        IAgentMiddleware middleware = new CompactionMiddleware(
            compactor,
            provider,
            PipelineTestContext.ScopeFor());
        var context = new AgentPipelineContext(
            new FakeAgentContext("alice", [remembered]),
            [new ModelTurn(ModelRole.User, "hi", DateTimeOffset.UnixEpoch)],
            CancellationToken.None);

        await middleware.InvokeAsync(
            context,
            (ctx, _) =>
            {
                nextCalled = true;
                ctx.Outcome = new IterationOutcome(Interrupted: false, ToolResultTurns: []);
                return Task.CompletedTask;
            },
            CancellationToken.None);

        await Assert.That(nextCalled).IsTrue();
        await Assert.That(compactAfterNext).IsTrue();
        await Assert.That(sent).IsNotNull();
        await Assert.That(sent!.Turns.Count).IsEqualTo(1);
        await Assert.That(sent.Turns[0]).IsEqualTo(remembered);
        await compactor.Received(1).CompactAsync(
            snapshot,
            Arg.Any<ModelPrompt>(),
            false,
            context.ShutdownToken);
    }

    [Test]
    public async Task SkipsCompactionWhenInterrupted()
    {
        var compactor = Substitute.For<IContextCompactor>();
        IAgentMiddleware middleware = new CompactionMiddleware(
            compactor,
            Substitute.For<IAgentContextProvider>(),
            PipelineTestContext.ScopeFor());
        var context = PipelineTestContext.Create();

        await middleware.InvokeAsync(
            context,
            (ctx, _) =>
            {
                ctx.Outcome = new IterationOutcome(Interrupted: true, ToolResultTurns: []);
                return Task.CompletedTask;
            },
            CancellationToken.None);

        await compactor.DidNotReceive().CompactAsync(
            Arg.Any<AgentContext>(),
            Arg.Any<ModelPrompt>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SkipsCompactionWhenOutcomeIsMissing()
    {
        var compactor = Substitute.For<IContextCompactor>();
        IAgentMiddleware middleware = new CompactionMiddleware(
            compactor,
            Substitute.For<IAgentContextProvider>(),
            PipelineTestContext.ScopeFor());

        await middleware.InvokeAsync(
            PipelineTestContext.Create(),
            (_, _) => Task.CompletedTask,
            CancellationToken.None);

        await compactor.DidNotReceive().CompactAsync(
            Arg.Any<AgentContext>(),
            Arg.Any<ModelPrompt>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ThrowsWhenTheContextSnapshotIsMissing()
    {
        var provider = Substitute.For<IAgentContextProvider>();
        provider
            .CreateAgentContextAsync(Arg.Any<SessionId>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<AgentContext?>(null));
        IAgentMiddleware middleware = new CompactionMiddleware(
            Substitute.For<IContextCompactor>(),
            provider,
            PipelineTestContext.ScopeFor());
        var context = PipelineTestContext.Create();

        await Assert.That(async () => await middleware.InvokeAsync(
                context,
                (ctx, _) =>
                {
                    ctx.Outcome = new IterationOutcome(Interrupted: false, ToolResultTurns: []);
                    return Task.CompletedTask;
                },
                CancellationToken.None))
            .Throws<InvalidOperationException>();
    }
}
