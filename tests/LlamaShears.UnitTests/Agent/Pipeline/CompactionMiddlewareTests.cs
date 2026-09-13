using System.Collections.Immutable;
using LlamaShears.Core;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Pipeline;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Common;
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
    private static readonly DateTimeOffset _now = DateTimeOffset.UnixEpoch;

    [Test]
    public async Task NoPlanSetsTheLivePromptAndCallsNextOnce()
    {
        var remembered = new ModelTurn(ModelRole.User, "remembered", _now);
        var inbound = new ModelTurn(ModelRole.User, "hi", _now);
        var scope = PipelineTestContext.ScopeFor();
        var original = scope.GetAgentConfig();
        var nextCount = 0;
        ModelPrompt? seen = null;
        var middleware = BuildMiddleware(scope, plan: null);

        await middleware.InvokeAsync(
            BuildContext([remembered], [inbound]),
            (ctx, _) =>
            {
                nextCount++;
                seen = ctx.Prompt;
                return Task.CompletedTask;
            },
            CancellationToken.None);

        await Assert.That(nextCount).IsEqualTo(1);
        await Assert.That(seen).IsNotNull();
        await Assert.That(seen!.Turns).IsEquivalentTo([remembered]);
        await Assert.That(scope.GetAgentConfig()).IsEqualTo(original);
        await Assert.That(scope.GetAgentConfig().PromptContext).IsEqualTo(original.PromptContext);
    }

    [Test]
    public async Task NoPlanAndCompactionOnlyDoesNotCallNext()
    {
        var nextCalled = false;
        var middleware = BuildMiddleware(PipelineTestContext.ScopeFor(), plan: null);
        var context = BuildContext([], [new ModelTurn(ModelRole.User, "hi", _now)]);
        context.CompactionOnly = true;

        await middleware.InvokeAsync(
            context,
            (_, _) =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            CancellationToken.None);

        await Assert.That(nextCalled).IsFalse();
        await Assert.That(context.Prompt).IsNull();
    }

    [Test]
    public async Task PlanOverlaysSystemAndEphemeralTemplatesThenRestoresThem()
    {
        var transcript = new ModelTurn(ModelRole.User, "<turn role=\"user\">older</turn>", _now);
        var preserved = new ModelTurn(ModelRole.User, "keep-me", _now);
        var plan = new CompactionPlan(null, transcript, [preserved]);
        var rebuilt = new ModelPrompt([
            new ModelTurn(ModelRole.Assistant, "the summary", _now),
            preserved,
        ]);
        var scope = PipelineTestContext.ScopeFor();
        var original = scope.GetAgentConfig();
        var nextCount = 0;
        AgentConfig? firstConfig = null;
        ModelPrompt? firstPrompt = null;
        AgentConfig? secondConfig = null;
        var (middleware, compactor) = BuildMiddlewareWithCompactor(scope, plan, rebuilt);
        var context = BuildContext([preserved], [new ModelTurn(ModelRole.User, "hi", _now)]);

        await middleware.InvokeAsync(
            context,
            (ctx, _) =>
            {
                nextCount++;
                if (nextCount == 1)
                {
                    firstConfig = scope.GetAgentConfig();
                    firstPrompt = ctx.Prompt;
                    ctx.Outcome = new IterationOutcome(
                        Interrupted: false,
                        ToolResultTurns: [],
                        Content: "the summary");
                }
                else
                {
                    secondConfig = scope.GetAgentConfig();
                }
                return Task.CompletedTask;
            },
            CancellationToken.None);

        await Assert.That(nextCount).IsEqualTo(2);
        await Assert.That(firstConfig!.SystemPrompt).IsEqualTo("COMPACTION.md");
        await Assert.That(firstConfig.PromptContext).IsEqualTo("COMPACTION.md");
        await Assert.That(firstPrompt!.Turns).IsEquivalentTo([transcript]);
        await Assert.That(firstPrompt.Turns.Any(t => t.Role is ModelRole.System or ModelRole.SystemEphemeral))
            .IsFalse();
        await Assert.That(secondConfig).IsEqualTo(original);
        await Assert.That(scope.GetAgentConfig()).IsEqualTo(original);
        await Assert.That(context.Prompt).IsEqualTo(rebuilt);
        await compactor.Received(1).CommitAsync(plan, "the summary", context.TurnToken);
    }

    [Test]
    public async Task CompactionOnlyStopsAfterTheSummarizerPass()
    {
        var transcript = new ModelTurn(ModelRole.User, "older", _now);
        var plan = new CompactionPlan(null, transcript, []);
        var nextCount = 0;
        var middleware = BuildMiddleware(
            PipelineTestContext.ScopeFor(),
            plan,
            new ModelPrompt([new ModelTurn(ModelRole.Assistant, "summary", _now)]));
        var context = BuildContext([], []);
        context.CompactionOnly = true;

        await middleware.InvokeAsync(
            context,
            (ctx, _) =>
            {
                nextCount++;
                ctx.Outcome = new IterationOutcome(
                    Interrupted: false,
                    ToolResultTurns: [],
                    Content: "summary");
                return Task.CompletedTask;
            },
            CancellationToken.None);

        await Assert.That(nextCount).IsEqualTo(1);
    }

    [Test]
    public async Task EmptySummaryThrows()
    {
        var plan = new CompactionPlan(null, new ModelTurn(ModelRole.User, "older", _now), []);
        var middleware = BuildMiddleware(PipelineTestContext.ScopeFor(), plan);

        await Assert.That(async () => await middleware.InvokeAsync(
                BuildContext([], [new ModelTurn(ModelRole.User, "hi", _now)]),
                (ctx, _) =>
                {
                    ctx.Outcome = new IterationOutcome(
                        Interrupted: false,
                        ToolResultTurns: [],
                        Content: "  ");
                    return Task.CompletedTask;
                },
                CancellationToken.None))
            .Throws<CompactionFailedException>();
    }

    [Test]
    public async Task InterruptThrows()
    {
        var plan = new CompactionPlan(null, new ModelTurn(ModelRole.User, "older", _now), []);
        var middleware = BuildMiddleware(PipelineTestContext.ScopeFor(), plan);

        await Assert.That(async () => await middleware.InvokeAsync(
                BuildContext([], [new ModelTurn(ModelRole.User, "hi", _now)]),
                (ctx, _) =>
                {
                    ctx.Outcome = new IterationOutcome(Interrupted: true, ToolResultTurns: []);
                    return Task.CompletedTask;
                },
                CancellationToken.None))
            .Throws<CompactionFailedException>();
    }

    [Test]
    public async Task PromptKeepsImageAttachmentsFromLiveTurnsWhenNotCompacting()
    {
        var image = new Attachment(AttachmentKind.Image, "image/png", "Zm9v");
        var remembered = new ModelTurn(ModelRole.User, "see this", _now)
        {
            Attachments = [image],
        };
        ModelPrompt? seen = null;
        var middleware = BuildMiddleware(PipelineTestContext.ScopeFor(), plan: null);

        await middleware.InvokeAsync(
            BuildContext([remembered], [new ModelTurn(ModelRole.User, "hi", _now)]),
            (ctx, _) =>
            {
                seen = ctx.Prompt;
                return Task.CompletedTask;
            },
            CancellationToken.None);

        await Assert.That(seen).IsNotNull();
        await Assert.That(seen!.Turns[0].Attachments).IsEquivalentTo([image]);
    }

    [Test]
    public async Task PublishesInboundBatchTurns()
    {
        var bus = Substitute.For<IEventBus>();
        var scope = PipelineTestContext.ScopeFor();
        var inbound = new ModelTurn(ModelRole.User, "hi", _now);
        var middleware = BuildMiddleware(scope, plan: null, bus: bus);
        var context = BuildContext([], [inbound]);
        context.CorrelationId = Guid.CreateVersion7();

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

    private static IAgentMiddleware BuildMiddleware(
        IDataContextScope scope,
        CompactionPlan? plan,
        ModelPrompt? rebuilt = null,
        IEventBus? bus = null)
        => BuildMiddlewareWithCompactor(scope, plan, rebuilt, bus).Middleware;

    private static (IAgentMiddleware Middleware, IContextCompactor Compactor) BuildMiddlewareWithCompactor(
        IDataContextScope scope,
        CompactionPlan? plan,
        ModelPrompt? rebuilt = null,
        IEventBus? bus = null)
    {
        var provider = Substitute.For<IAgentContextProvider>();
        provider
            .CreateAgentContextAsync(Arg.Any<SessionId>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<AgentContext?>(TestAgentConfigs.BuildAgentContext("alice")));
        var compactor = Substitute.For<IContextCompactor>();
        compactor
            .TryPrepareAsync(
                Arg.Any<AgentContext>(),
                Arg.Any<ModelPrompt>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(plan));
        if (rebuilt is not null)
        {
            compactor
                .CommitAsync(Arg.Any<CompactionPlan>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(ValueTask.FromResult(rebuilt));
        }
        IAgentMiddleware middleware = new CompactionMiddleware(
            compactor,
            provider,
            bus ?? Substitute.For<IEventBus>(),
            scope);
        return (middleware, compactor);
    }

    private static AgentPipelineContext BuildContext(
        IReadOnlyList<ModelTurn> liveTurns,
        ImmutableArray<ModelTurn> batch)
        => new(new FakeAgentContext("alice", liveTurns), batch, CancellationToken.None);
}
