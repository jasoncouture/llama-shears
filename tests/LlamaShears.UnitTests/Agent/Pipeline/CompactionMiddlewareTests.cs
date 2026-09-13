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
        var nextCount = 0;
        ModelPrompt? seen = null;
        var (middleware, runner, _) = Build(plan: null);

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
        await Assert.That(seen!.Turns).IsEquivalentTo([remembered]);
        await runner.DidNotReceive()
            .RunAsync(Arg.Any<SubagentRunRequest>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task NoPlanAndCompactionOnlyDoesNotCallNext()
    {
        var nextCalled = false;
        var (middleware, _, _) = Build(plan: null);
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
    public async Task PlanSpawnsACompactionChildThenCommitsAndCallsNext()
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
        SubagentRunRequest? sent = null;
        var nextCount = 0;
        var (middleware, runner, compactor) = Build(
            plan,
            rebuilt,
            scope,
            result: Succeeded("the summary"),
            capture: request => sent = request);

        await middleware.InvokeAsync(
            BuildContext([preserved], [new ModelTurn(ModelRole.User, "hi", _now)]),
            (ctx, _) =>
            {
                nextCount++;
                return Task.CompletedTask;
            },
            CancellationToken.None);

        await Assert.That(nextCount).IsEqualTo(1);
        await Assert.That(sent).IsNotNull();
        await Assert.That(sent!.Prompt).IsEqualTo(transcript.Content);
        await Assert.That(sent.SystemPrompt).IsEqualTo("COMPACTION.md");
        await Assert.That(sent.PromptContext).IsEqualTo("COMPACTION.md");
        await Assert.That(sent.AwaitResult).IsTrue();
        await Assert.That(sent.MaxTurns).IsEqualTo(5);
        await Assert.That(scope.GetAgentConfig()).IsEqualTo(original);
        await compactor.Received(1).CommitAsync(plan, "the summary", Arg.Any<CancellationToken>());
        await runner.Received(1).RunAsync(Arg.Any<SubagentRunRequest>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CompactionOnlyStopsAfterCommit()
    {
        var plan = new CompactionPlan(null, new ModelTurn(ModelRole.User, "older", _now), []);
        var nextCount = 0;
        var (middleware, _, compactor) = Build(
            plan,
            new ModelPrompt([new ModelTurn(ModelRole.Assistant, "summary", _now)]),
            result: Succeeded("summary"));
        var context = BuildContext([], []);
        context.CompactionOnly = true;

        await middleware.InvokeAsync(
            context,
            (_, _) =>
            {
                nextCount++;
                return Task.CompletedTask;
            },
            CancellationToken.None);

        await Assert.That(nextCount).IsEqualTo(0);
        await compactor.Received(1).CommitAsync(plan, "summary", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task EmptySummaryThrows()
    {
        var plan = new CompactionPlan(null, new ModelTurn(ModelRole.User, "older", _now), []);
        var (middleware, _, compactor) = Build(plan, result: Succeeded("  "));

        await Assert.That(async () => await middleware.InvokeAsync(
                BuildContext([], [new ModelTurn(ModelRole.User, "hi", _now)]),
                (_, _) => Task.CompletedTask,
                CancellationToken.None))
            .Throws<CompactionFailedException>();
        await compactor.DidNotReceive()
            .CommitAsync(Arg.Any<CompactionPlan>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task FailedChildThrows()
    {
        var plan = new CompactionPlan(null, new ModelTurn(ModelRole.User, "older", _now), []);
        var (middleware, _, _) = Build(
            plan,
            result: new SubagentRunResult(
                Ok: false,
                SessionId: "alice:x:subagent-1",
                Awaited: true,
                Output: null,
                TimedOut: true,
                Error: "Timed out after 120s waiting for the sub-agent.",
                Started: true));

        await Assert.That(async () => await middleware.InvokeAsync(
                BuildContext([], [new ModelTurn(ModelRole.User, "hi", _now)]),
                (_, _) => Task.CompletedTask,
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
        var (middleware, _, _) = Build(plan: null);

        await middleware.InvokeAsync(
            BuildContext([remembered], [new ModelTurn(ModelRole.User, "hi", _now)]),
            (ctx, _) =>
            {
                seen = ctx.Prompt;
                return Task.CompletedTask;
            },
            CancellationToken.None);

        await Assert.That(seen!.Turns[0].Attachments).IsEquivalentTo([image]);
    }

    [Test]
    public async Task PublishesInboundBatchTurns()
    {
        var bus = Substitute.For<IEventBus>();
        var scope = PipelineTestContext.ScopeFor();
        var inbound = new ModelTurn(ModelRole.User, "hi", _now);
        var (middleware, _, _) = Build(plan: null, scope: scope, bus: bus);
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
            Substitute.For<ISubagentRunner>(),
            provider,
            Substitute.For<IEventBus>(),
            PipelineTestContext.ScopeFor());

        await Assert.That(async () => await middleware.InvokeAsync(
                PipelineTestContext.Create(),
                (_, _) => Task.CompletedTask,
                CancellationToken.None))
            .Throws<InvalidOperationException>();
    }

    private static (IAgentMiddleware Middleware, ISubagentRunner Runner, IContextCompactor Compactor) Build(
        CompactionPlan? plan,
        ModelPrompt? rebuilt = null,
        IDataContextScope? scope = null,
        IEventBus? bus = null,
        SubagentRunResult? result = null,
        Action<SubagentRunRequest>? capture = null)
    {
        scope ??= PipelineTestContext.ScopeFor();
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

        var runner = Substitute.For<ISubagentRunner>();
        runner
            .RunAsync(Arg.Any<SubagentRunRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                capture?.Invoke(call.Arg<SubagentRunRequest>());
                return ValueTask.FromResult(result ?? Succeeded("summary"));
            });

        IAgentMiddleware middleware = new CompactionMiddleware(
            compactor,
            runner,
            provider,
            bus ?? Substitute.For<IEventBus>(),
            scope);
        return (middleware, runner, compactor);
    }

    private static SubagentRunResult Succeeded(string output) =>
        new(
            Ok: true,
            SessionId: "alice:x:subagent-1",
            Awaited: true,
            Output: output,
            TimedOut: false,
            Error: null,
            Started: true);

    private static AgentPipelineContext BuildContext(
        IReadOnlyList<ModelTurn> liveTurns,
        ImmutableArray<ModelTurn> batch)
        => new(new FakeAgentContext("alice", liveTurns), batch, CancellationToken.None);
}
