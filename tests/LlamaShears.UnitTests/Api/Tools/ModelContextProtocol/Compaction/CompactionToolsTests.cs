using LlamaShears.Api.Tools.ModelContextProtocol.Compaction;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Context;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.UnitTests.Agent.Core;
using LlamaShears.UnitTests.Agent.Pipeline;
using NSubstitute;

namespace LlamaShears.UnitTests.Api.Tools.ModelContextProtocol.Compaction;

public sealed class CompactionToolsTests
{
    private static readonly DateTimeOffset _now = DateTimeOffset.UnixEpoch;

    [Test]
    public async Task MissingAgentIsRefused()
    {
        var scope = PipelineTestContext.ScopeFor();
        scope.SetItem(AgentConfig.DataKey, null);
        var (tools, runner, compactor) = Build(scope);

        var result = await tools.CompactContext();

        await Assert.That(result.Compacted).IsFalse();
        await Assert.That(result.Error).Contains("authenticated agent");
        await runner.DidNotReceive()
            .RunAsync(Arg.Any<SubagentRunRequest>(), Arg.Any<CancellationToken>());
        await compactor.DidNotReceive()
            .TryPrepareAsync(
                Arg.Any<AgentContext>(),
                Arg.Any<ModelPrompt>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task NoPlanReturnsNotCompacted()
    {
        var (tools, runner, compactor) = Build(plan: null);

        var result = await tools.CompactContext();

        await Assert.That(result.Compacted).IsFalse();
        await Assert.That(result.Error).IsNull();
        await runner.DidNotReceive()
            .RunAsync(Arg.Any<SubagentRunRequest>(), Arg.Any<CancellationToken>());
        await compactor.Received(1).TryPrepareAsync(
            Arg.Any<AgentContext>(),
            Arg.Any<ModelPrompt>(),
            true,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task PlanSpawnsCompactionChildAndCommits()
    {
        var transcript = new ModelTurn(ModelRole.User, "<turn role=\"user\">older</turn>", _now);
        var preserved = new ModelTurn(ModelRole.User, "keep-me", _now);
        var plan = new CompactionPlan(null, transcript, [preserved]);
        SubagentRunRequest? sent = null;
        var (tools, runner, compactor) = Build(
            plan,
            Succeeded("the summary"),
            request => sent = request);

        var result = await tools.CompactContext();

        await Assert.That(result.Compacted).IsTrue();
        await Assert.That(result.PreservedTurns).IsEqualTo(1);
        await Assert.That(result.Error).IsNull();
        await Assert.That(sent!.Prompt).IsEqualTo(transcript.Content);
        await Assert.That(sent.SystemPrompt).IsEqualTo("COMPACTION.md");
        await Assert.That(sent.PromptContext).IsEqualTo("COMPACTION.md");
        await Assert.That(sent.AwaitResult).IsTrue();
        await Assert.That(sent.MaxTurns).IsEqualTo(5);
        await compactor.Received(1).CommitAsync(plan, "the summary", Arg.Any<CancellationToken>());
        await runner.Received(1).RunAsync(Arg.Any<SubagentRunRequest>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task FailedChildDoesNotCommit()
    {
        var plan = new CompactionPlan(null, new ModelTurn(ModelRole.User, "older", _now), []);
        var (tools, _, compactor) = Build(
            plan,
            new SubagentRunResult(
                Ok: false,
                SessionId: "alice:x:subagent-1",
                Awaited: true,
                Output: null,
                TimedOut: true,
                Error: "Timed out after 120s waiting for the sub-agent.",
                Started: true));

        var result = await tools.CompactContext();

        await Assert.That(result.Compacted).IsFalse();
        await Assert.That(result.Error).Contains("Timed out");
        await compactor.DidNotReceive()
            .CommitAsync(Arg.Any<CompactionPlan>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task EmptySummaryDoesNotCommit()
    {
        var plan = new CompactionPlan(null, new ModelTurn(ModelRole.User, "older", _now), []);
        var (tools, _, compactor) = Build(plan, Succeeded("  "));

        var result = await tools.CompactContext();

        await Assert.That(result.Compacted).IsFalse();
        await Assert.That(result.Error).Contains("empty summary");
        await compactor.DidNotReceive()
            .CommitAsync(Arg.Any<CompactionPlan>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private static (CompactionTools Tools, ISubagentRunner Runner, IContextCompactor Compactor) Build(
        CompactionPlan? plan = null,
        SubagentRunResult? result = null,
        Action<SubagentRunRequest>? capture = null)
        => Build(PipelineTestContext.ScopeFor(), plan, result, capture);

    private static (CompactionTools Tools, ISubagentRunner Runner, IContextCompactor Compactor) Build(
        IDataContextScope scope,
        CompactionPlan? plan = null,
        SubagentRunResult? result = null,
        Action<SubagentRunRequest>? capture = null)
    {
        var snapshot = TestAgentConfigs.BuildAgentContext("alice");
        var provider = Substitute.For<IAgentContextProvider>();
        provider
            .CreateAgentContextAsync(Arg.Any<SessionId>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<AgentContext?>(snapshot));
        var compactor = Substitute.For<IContextCompactor>();
        compactor
            .TryPrepareAsync(
                Arg.Any<AgentContext>(),
                Arg.Any<ModelPrompt>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(plan));
        var runner = Substitute.For<ISubagentRunner>();
        runner
            .RunAsync(Arg.Any<SubagentRunRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                capture?.Invoke(call.Arg<SubagentRunRequest>());
                return ValueTask.FromResult(result ?? Succeeded("summary"));
            });
        var tools = new CompactionTools(
            scope,
            provider,
            compactor,
            runner,
            Substitute.For<IEventBus>());
        return (tools, runner, compactor);
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
}
