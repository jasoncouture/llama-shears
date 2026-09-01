using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Pipeline;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Content;
using LlamaShears.Core.Abstractions.Context;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Core.Pipeline;
using LlamaShears.UnitTests.Agent.Core;
using NSubstitute;

namespace LlamaShears.UnitTests.Agent.Pipeline;

public sealed class CompactionMiddlewareTests
{
    [Test]
    public async Task CompactsAndWritesThePromptBeforeNext()
    {
        var nextCalled = false;
        var compactBeforeNext = false;
        var remembered = new ModelTurn(ModelRole.User, "remembered", DateTimeOffset.UnixEpoch);
        var system = new ModelTurn(ModelRole.System, "persona", DateTimeOffset.UnixEpoch);
        var snapshot = TestAgentConfigs.BuildAgentContext("alice");
        var provider = Substitute.For<IAgentContextProvider>();
        provider
            .CreateAgentContextAsync(Arg.Any<SessionId>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<AgentContext?>(snapshot));
        var compacted = new ModelPrompt([system, remembered]);
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
                compactBeforeNext = !nextCalled;
                sent = call.Arg<ModelPrompt>();
                return ValueTask.FromResult(compacted);
            });
        IAgentMiddleware middleware = new CompactionMiddleware(
            compactor,
            provider,
            Substitute.For<IEventBus>(),
            PipelineTestContext.ScopeFor());
        var context = new AgentPipelineContext(
            new FakeAgentContext("alice", [remembered]),
            [new ModelTurn(ModelRole.User, "hi", DateTimeOffset.UnixEpoch)],
            CancellationToken.None)
        {
            SystemPrompt = system,
        };

        await middleware.InvokeAsync(
            context,
            (_, _) =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            CancellationToken.None);

        await Assert.That(nextCalled).IsTrue();
        await Assert.That(compactBeforeNext).IsTrue();
        await Assert.That(sent).IsNotNull();
        await Assert.That(sent!.Turns[0]).IsEqualTo(system);
        await Assert.That(sent.Turns[1]).IsEqualTo(remembered);
        await Assert.That(context.Prompt).IsEqualTo(compacted);
        await compactor.Received(1).CompactAsync(
            snapshot,
            Arg.Any<ModelPrompt>(),
            false,
            context.TurnToken);
    }

    [Test]
    public async Task OmitsSystemWhenTheBagHasNone()
    {
        var remembered = new ModelTurn(ModelRole.User, "remembered", DateTimeOffset.UnixEpoch);
        var provider = Substitute.For<IAgentContextProvider>();
        provider
            .CreateAgentContextAsync(Arg.Any<SessionId>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<AgentContext?>(TestAgentConfigs.BuildAgentContext("alice")));
        ModelPrompt? sent = null;
        var compactor = Substitute.For<IContextCompactor>();
        compactor
            .CompactAsync(
                Arg.Any<AgentContext>(),
                Arg.Any<ModelPrompt>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                sent = call.Arg<ModelPrompt>();
                return ValueTask.FromResult(call.Arg<ModelPrompt>());
            });
        IAgentMiddleware middleware = new CompactionMiddleware(
            compactor,
            provider,
            Substitute.For<IEventBus>(),
            PipelineTestContext.ScopeFor());
        var context = new AgentPipelineContext(
            new FakeAgentContext("alice", [remembered]),
            [new ModelTurn(ModelRole.User, "hi", DateTimeOffset.UnixEpoch)],
            CancellationToken.None);

        await middleware.InvokeAsync(context, (_, _) => Task.CompletedTask, CancellationToken.None);

        await Assert.That(sent).IsNotNull();
        await Assert.That(sent!.Turns[0]).IsEqualTo(remembered);
        await Assert.That(context.Prompt).IsEqualTo(sent);
    }

    [Test]
    public async Task PromptKeepsImageAttachmentsFromLiveTurns()
    {
        var image = new Attachment(AttachmentKind.Image, "image/png", "Zm9v");
        var remembered = new ModelTurn(ModelRole.User, "see this", DateTimeOffset.UnixEpoch)
        {
            Attachments = [image],
        };
        var provider = Substitute.For<IAgentContextProvider>();
        provider
            .CreateAgentContextAsync(Arg.Any<SessionId>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<AgentContext?>(TestAgentConfigs.BuildAgentContext("alice")));
        ModelPrompt? sent = null;
        var compactor = Substitute.For<IContextCompactor>();
        compactor
            .CompactAsync(
                Arg.Any<AgentContext>(),
                Arg.Any<ModelPrompt>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                sent = call.Arg<ModelPrompt>();
                return ValueTask.FromResult(call.Arg<ModelPrompt>());
            });
        IAgentMiddleware middleware = new CompactionMiddleware(
            compactor,
            provider,
            Substitute.For<IEventBus>(),
            PipelineTestContext.ScopeFor());
        var context = new AgentPipelineContext(
            new FakeAgentContext("alice", [remembered]),
            [new ModelTurn(ModelRole.User, "hi", DateTimeOffset.UnixEpoch)],
            CancellationToken.None);

        await middleware.InvokeAsync(context, (_, _) => Task.CompletedTask, CancellationToken.None);

        await Assert.That(sent).IsNotNull();
        await Assert.That(sent!.Turns[0].Attachments).IsEquivalentTo([image]);
    }

    [Test]
    public async Task PublishesInboundBatchTurns()
    {
        var bus = Substitute.For<IEventBus>();
        var provider = Substitute.For<IAgentContextProvider>();
        provider
            .CreateAgentContextAsync(Arg.Any<SessionId>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<AgentContext?>(TestAgentConfigs.BuildAgentContext("alice")));
        var compactor = Substitute.For<IContextCompactor>();
        compactor
            .CompactAsync(
                Arg.Any<AgentContext>(),
                Arg.Any<ModelPrompt>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(call => ValueTask.FromResult(call.Arg<ModelPrompt>()));
        var scope = PipelineTestContext.ScopeFor();
        IAgentMiddleware middleware = new CompactionMiddleware(compactor, provider, bus, scope);
        var inbound = new ModelTurn(ModelRole.User, "hi", DateTimeOffset.UnixEpoch);
        var context = new AgentPipelineContext(
            new FakeAgentContext("alice"),
            [inbound],
            CancellationToken.None)
        {
            CorrelationId = Guid.CreateVersion7(),
        };

        await middleware.InvokeAsync(context, (_, _) => Task.CompletedTask, CancellationToken.None);

        await bus.Received(1).PublishAsync(
            Event.WellKnown.Agent.Turn with { Id = scope.GetCurrentSessionId() },
            inbound,
            context.CorrelationId,
            context.ShutdownToken);
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
            Substitute.For<IEventBus>(),
            PipelineTestContext.ScopeFor());

        await Assert.That(async () => await middleware.InvokeAsync(
                PipelineTestContext.Create(),
                (_, _) => Task.CompletedTask,
                CancellationToken.None))
            .Throws<InvalidOperationException>();
    }
}
