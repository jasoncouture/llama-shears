using LlamaShears.Core;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Persistence;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.Context;
using LlamaShears.Core.Abstractions.Provider;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace LlamaShears.UnitTests.Agent.Core;

public sealed class ContextCompactorTests
{
    private static readonly DateTimeOffset _now = DateTimeOffset.UnixEpoch;
    private const string OlderUser = "OLDER_USER";
    private const string OlderAssistant = "OLDER_ASSISTANT";
    private const string KeepPrefix = "KEEP_";

    [Test]
    public async Task SixOrFewerEligibleTurnsReturnsNoPlan()
    {
        var config = new ModelConfiguration(new CompositeIdentity("ollama", "test"), ContextLength: 50);
        var compactor = BuildCompactor(config);
        var prompt = new ModelPrompt([
            new ModelTurn(ModelRole.System, "you are a helpful agent", _now),
            new ModelTurn(ModelRole.User, "hi", _now),
            new ModelTurn(ModelRole.Assistant, "hi back", _now),
            new ModelTurn(ModelRole.User, "what's up", _now),
            new ModelTurn(ModelRole.Assistant, "not much", _now),
            new ModelTurn(ModelRole.User, "ok", _now),
            new ModelTurn(ModelRole.Assistant, "ok", _now),
        ]);

        var plan = await compactor.TryPrepareAsync(BuildAgentContext(prompt, config), prompt, force: true, CancellationToken.None);

        await Assert.That(plan).IsNull();
    }

    [Test]
    public async Task NoContextLengthReturnsNoPlan()
    {
        var config = new ModelConfiguration(new CompositeIdentity("ollama", "test"), ContextLength: null);
        var compactor = BuildCompactor(config);
        var prompt = LongPromptOver(charsPerTurn: 10_000);

        var plan = await compactor.TryPrepareAsync(BuildAgentContext(prompt, config), prompt, force: false, CancellationToken.None);

        await Assert.That(plan).IsNull();
    }

    [Test]
    public async Task UnderBudgetReturnsNoPlan()
    {
        var config = new ModelConfiguration(new CompositeIdentity("ollama", "test"), ContextLength: 100_000, TokenLimit: 100);
        var compactor = BuildCompactor(config);
        var prompt = ShortPromptWithEightEligibleTurns();

        var plan = await compactor.TryPrepareAsync(BuildAgentContext(prompt, config), prompt, force: false, CancellationToken.None);

        await Assert.That(plan).IsNull();
    }

    [Test]
    public async Task OverBudgetPreparesTranscriptOfOlderTurnsAndPreservesLastSix()
    {
        var config = new ModelConfiguration(new CompositeIdentity("ollama", "test"), ContextLength: 1_000, TokenLimit: 100);
        var compactor = BuildCompactor(config);
        var prompt = LongPromptOver(charsPerTurn: 2_000);

        var plan = await compactor.TryPrepareAsync(BuildAgentContext(prompt, config), prompt, force: false, CancellationToken.None);

        await Assert.That(plan).IsNotNull();
        await Assert.That(plan!.Transcript.Role).IsEqualTo(ModelRole.User);
        await Assert.That(plan.Transcript.Content).Contains(OlderUser);
        await Assert.That(plan.Transcript.Content).Contains(OlderAssistant);
        await Assert.That(plan.Transcript.Content).DoesNotContain($"{KeepPrefix}0");
        await Assert.That(plan.Preserved.Length).IsEqualTo(6);
        await Assert.That(plan.Preserved[0].Content).IsEqualTo($"{KeepPrefix}0");
        await Assert.That(plan.SystemTurn!.Content).IsEqualTo(prompt.Turns[0].Content);
    }

    [Test]
    public async Task ForcePrepareKeepsLastSixWhenTailIsNotUser()
    {
        var config = new ModelConfiguration(new CompositeIdentity("ollama", "test"), ContextLength: 1_000, TokenLimit: 100);
        var compactor = BuildCompactor(config);
        var prompt = LongPromptOver(charsPerTurn: 2_000);

        var plan = await compactor.TryPrepareAsync(BuildAgentContext(prompt, config), prompt, force: true, CancellationToken.None);

        await Assert.That(plan).IsNotNull();
        await Assert.That(plan!.Preserved[^1].Role).IsEqualTo(ModelRole.Assistant);
        await Assert.That(plan.Preserved[^1].Content).IsEqualTo(prompt.Turns[^1].Content);
    }

    [Test]
    public async Task ToolCutWalksBackToTheMatchingAssistantCall()
    {
        var config = new ModelConfiguration(new CompositeIdentity("ollama", "test"), ContextLength: 1_000, TokenLimit: 100);
        var compactor = BuildCompactor(config);
        var issuer = new ModelTurn(ModelRole.Assistant, "issuer", _now)
        {
            ToolCalls = [new ToolCall(ToolCall.InternalToolSource, "memory_store", "{\"k\":1}", "c1")],
        };
        var tool = new ModelTurn(ModelRole.Tool, "tool-result-body", _now) { ToolCall = issuer.ToolCalls[0] };
        var filler = new string('x', 2_000);
        var prompt = new ModelPrompt([
            new ModelTurn(ModelRole.System, "you are a helpful agent", _now),
            new ModelTurn(ModelRole.User, $"{OlderUser}{filler}", _now),
            issuer,
            tool,
            new ModelTurn(ModelRole.User, $"{KeepPrefix}0", _now),
            new ModelTurn(ModelRole.Assistant, $"{KeepPrefix}1", _now),
            new ModelTurn(ModelRole.User, $"{KeepPrefix}2", _now),
            new ModelTurn(ModelRole.Assistant, $"{KeepPrefix}3", _now),
            new ModelTurn(ModelRole.User, $"{KeepPrefix}4", _now),
        ]);

        var plan = await compactor.TryPrepareAsync(BuildAgentContext(prompt, config), prompt, force: false, CancellationToken.None);

        await Assert.That(plan).IsNotNull();
        await Assert.That(plan!.Preserved[0]).IsEqualTo(issuer);
        await Assert.That(plan.Preserved[1]).IsEqualTo(tool);
        await Assert.That(plan.Preserved[^1].Content).IsEqualTo($"{KeepPrefix}4");
        await Assert.That(plan.Transcript.Content).Contains(OlderUser);
    }

    [Test]
    public async Task CommitWritesSummaryAndPreservedTurns()
    {
        var config = new ModelConfiguration(new CompositeIdentity("ollama", "test"), ContextLength: 1_000);
        var store = Substitute.For<IContextStore>();
        var live = Substitute.For<IAgentContext>();
        store.OpenAsync(Arg.Any<SessionId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(live));
        var appended = new List<ModelTurn>();
        live.AppendAsync(Arg.Any<IContextEntry>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                if (call.Arg<IContextEntry>() is ModelTurn turn)
                {
                    appended.Add(turn);
                }
                return Task.CompletedTask;
            });
        var compactor = BuildCompactor(config, store);
        var system = new ModelTurn(ModelRole.System, "persona", _now);
        var preserved = new ModelTurn(ModelRole.User, "keep-me", _now);
        var plan = new CompactionPlan(
            system,
            new ModelTurn(ModelRole.User, "transcript", _now),
            [preserved]);

        var rebuilt = await compactor.CommitAsync(plan, "the summary", CancellationToken.None);

        await Assert.That(rebuilt.Turns.Count).IsEqualTo(3);
        await Assert.That(rebuilt.Turns[0]).IsEqualTo(system);
        await Assert.That(rebuilt.Turns[1].Role).IsEqualTo(ModelRole.Assistant);
        await Assert.That(rebuilt.Turns[1].Content).IsEqualTo("the summary");
        await Assert.That(rebuilt.Turns[2]).IsEqualTo(preserved);
        await store.Received(1).ClearAsync(Arg.Any<SessionId>(), true, Arg.Any<CancellationToken>());
        await Assert.That(appended.Select(t => t.Content).ToArray())
            .IsEquivalentTo(["the summary", "keep-me"]);
    }

    private static IContextCompactor BuildCompactor(ModelConfiguration config, IContextStore? store = null)
    {
        if (store is null)
        {
            store = Substitute.For<IContextStore>();
            store.OpenAsync(Arg.Any<SessionId>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(Substitute.For<IAgentContext>()));
        }
        var agentConfig = new AgentConfig(Model: config, ModelContextProtocolServers: [], Id: "test")
        {
            HeartbeatPeriod = TimeSpan.Zero,
        };
        var dataScope = TestAgentConfigs.DataContextFactoryWith(agentConfig).Current!;
        return new ContextCompactor(store, dataScope, NullLogger<ContextCompactor>.Instance);
    }

    private static AgentContext BuildAgentContext(ModelPrompt prompt, ModelConfiguration config)
    {
        var totalEstimate = 0;
        foreach (var turn in prompt.Turns)
        {
            totalEstimate += (int)Math.Ceiling(turn.Content.Length * 1.5 / 2.0);
        }
        return new AgentContext(
            AgentId: "test",
            Now: _now,
            Config: new AgentConfig(Model: config, ModelContextProtocolServers: []),
            LanguageModel: new LanguageModelContext(
                Turns: [.. prompt.Turns],
                Entries: [.. prompt.Turns],
                ContextWindowTokenCount: totalEstimate),
            System: new SystemContext(),
            Tools: new ToolContext([]),
            Plugins: new PluginContext([]));
    }

    private static ModelPrompt ShortPromptWithEightEligibleTurns() =>
        new ModelPrompt([
            new ModelTurn(ModelRole.System, "you are a helpful agent", _now),
            new ModelTurn(ModelRole.User, "a", _now),
            new ModelTurn(ModelRole.Assistant, "b", _now),
            new ModelTurn(ModelRole.User, "c", _now),
            new ModelTurn(ModelRole.Assistant, "d", _now),
            new ModelTurn(ModelRole.User, "e", _now),
            new ModelTurn(ModelRole.Assistant, "f", _now),
            new ModelTurn(ModelRole.User, "g", _now),
            new ModelTurn(ModelRole.Assistant, "h", _now),
        ]);

    private static ModelPrompt LongPromptOver(int charsPerTurn)
    {
        var filler = new string('x', charsPerTurn);
        return new ModelPrompt([
            new ModelTurn(ModelRole.System, "you are a helpful agent", _now),
            new ModelTurn(ModelRole.User, OlderUser + filler, _now),
            new ModelTurn(ModelRole.Assistant, OlderAssistant + filler, _now),
            new ModelTurn(ModelRole.User, $"{KeepPrefix}0", _now),
            new ModelTurn(ModelRole.Assistant, $"{KeepPrefix}1", _now),
            new ModelTurn(ModelRole.User, $"{KeepPrefix}2", _now),
            new ModelTurn(ModelRole.Assistant, $"{KeepPrefix}3", _now),
            new ModelTurn(ModelRole.User, $"{KeepPrefix}4", _now),
            new ModelTurn(ModelRole.Assistant, $"{KeepPrefix}5", _now),
        ]);
    }
}
