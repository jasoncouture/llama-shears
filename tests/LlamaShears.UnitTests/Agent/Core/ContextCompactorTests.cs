using System.Collections.Immutable;
using LlamaShears.Core;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Persistence;
using LlamaShears.Core.Abstractions.Context;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Memory;
using LlamaShears.Core.Abstractions.PromptContext;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Core.Abstractions.SystemPrompt;
using LlamaShears.Core.Tools.ModelContextProtocol;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

using LlamaShears.Core.Abstractions.Common;
namespace LlamaShears.UnitTests.Agent.Core;

public sealed class ContextCompactorTests
{
    private static readonly DateTimeOffset _now = DateTimeOffset.UnixEpoch;

    [Test]
    public async Task BelowMinTurnsReturnsPromptUnchanged()
    {
        var model = BuildModel();
        var compactor = BuildCompactor();
        var prompt = new ModelPrompt([
            new ModelTurn(ModelRole.System, "you are a helpful agent", _now),
            new ModelTurn(ModelRole.User, "hi", _now),
            new ModelTurn(ModelRole.Assistant, "hi back", _now),
            new ModelTurn(ModelRole.User, "what's up", _now),
        ]);
        var config = new ModelConfiguration(new CompositeIdentity("ollama", "test"), ContextLength: 50);

        var result = await compactor.CompactAsync(BuildAgentContext(prompt, config), prompt, model, config, force: false, CancellationToken.None);

        await Assert.That(result).IsSameReferenceAs(prompt);
        _ = model.DidNotReceive().PromptAsync(
            Arg.Any<ModelPrompt>(), Arg.Any<PromptOptions?>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task NoContextLengthReturnsPromptUnchanged()
    {
        var model = BuildModel();
        var compactor = BuildCompactor();
        var prompt = LongPromptOver(charsPerTurn: 10_000);
        var config = new ModelConfiguration(new CompositeIdentity("ollama", "test"), ContextLength: null);

        var result = await compactor.CompactAsync(BuildAgentContext(prompt, config), prompt, model, config, force: false, CancellationToken.None);

        await Assert.That(result).IsSameReferenceAs(prompt);
        _ = model.DidNotReceive().PromptAsync(
            Arg.Any<ModelPrompt>(), Arg.Any<PromptOptions?>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UnderBudgetReturnsPromptUnchanged()
    {
        var model = BuildModel();
        var compactor = BuildCompactor();
        var prompt = ShortPromptWithFiveTurns();
        var config = new ModelConfiguration(new CompositeIdentity("ollama", "test"), ContextLength: 100_000, TokenLimit: 100);

        var result = await compactor.CompactAsync(BuildAgentContext(prompt, config), prompt, model, config, force: false, CancellationToken.None);

        await Assert.That(result).IsSameReferenceAs(prompt);
        _ = model.DidNotReceive().PromptAsync(
            Arg.Any<ModelPrompt>(), Arg.Any<PromptOptions?>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task OverBudgetCompactsToSystemAssistantSummaryAndPreservedUserTurn()
    {
        var model = BuildModel(summary: "here is the summary");
        var compactor = BuildCompactor();
        var prompt = LongPromptOver(charsPerTurn: 2_000);
        var config = new ModelConfiguration(new CompositeIdentity("ollama", "test"), ContextLength: 1_000, TokenLimit: 100);

        var result = await compactor.CompactAsync(BuildAgentContext(prompt, config), prompt, model, config, force: false, CancellationToken.None);

        await Assert.That(result).IsNotSameReferenceAs(prompt);
        await Assert.That(result.Turns.Count).IsEqualTo(3);
        await Assert.That(result.Turns[0].Role).IsEqualTo(ModelRole.System);
        await Assert.That(result.Turns[0].Content).IsEqualTo(prompt.Turns[0].Content);
        await Assert.That(result.Turns[1].Role).IsEqualTo(ModelRole.Assistant);
        await Assert.That(result.Turns[1].Content).IsEqualTo("here is the summary");
        await Assert.That(result.Turns[2].Role).IsEqualTo(ModelRole.User);
        await Assert.That(result.Turns[2].Content).IsEqualTo(prompt.Turns[^1].Content);
    }

    [Test]
    public async Task SummarizationUsesCappedTokenLimit()
    {
        var model = BuildModel(summary: "a summary");
        var compactor = BuildCompactor();
        var prompt = LongPromptOver(charsPerTurn: 2_000);
        var config = new ModelConfiguration(new CompositeIdentity("ollama", "test"), ContextLength: 900, TokenLimit: 100);

        await compactor.CompactAsync(BuildAgentContext(prompt, config), prompt, model, config, force: false, CancellationToken.None);

        _ = model.Received().PromptAsync(
            Arg.Any<ModelPrompt>(),
            Arg.Is<PromptOptions?>(o => o!.TokenLimit == 300),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SummarizationFloorsAtMinTokenLimit()
    {
        var model = BuildModel(summary: "a summary");
        var compactor = BuildCompactor();
        var prompt = LongPromptOver(charsPerTurn: 2_000);
        var config = new ModelConfiguration(new CompositeIdentity("ollama", "test"), ContextLength: 600, TokenLimit: 100);

        await compactor.CompactAsync(BuildAgentContext(prompt, config), prompt, model, config, force: false, CancellationToken.None);

        _ = model.Received().PromptAsync(
            Arg.Any<ModelPrompt>(),
            Arg.Is<PromptOptions?>(o => o!.TokenLimit == 256),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task EmptySummaryThrowsCompactionFailed()
    {
        var model = BuildModel(summary: "   ");
        var compactor = BuildCompactor();
        var prompt = LongPromptOver(charsPerTurn: 2_000);
        var config = new ModelConfiguration(new CompositeIdentity("ollama", "test"), ContextLength: 1_000, TokenLimit: 100);

        await Assert.That(async () => await compactor.CompactAsync(BuildAgentContext(prompt, config), prompt, model, config, force: false, CancellationToken.None))
            .Throws<CompactionFailedException>();
    }

    [Test]
    public async Task SummarizationPromptDropsTrailingUserAndAddsCompactionSystemAfterAgentSystem()
    {
        var capturedPrompts = new List<ModelPrompt>();
        var model = Substitute.For<ILanguageModel>();
        StubEstimate(model);
        model.PromptAsync(Arg.Any<ModelPrompt>(), Arg.Any<PromptOptions?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                capturedPrompts.Add(call.Arg<ModelPrompt>());
                return AsyncEnum<IModelResponseFragment>(new TextFragment("ok"));
            });
        var compactor = BuildCompactor();
        var prompt = LongPromptOver(charsPerTurn: 2_000);
        var config = new ModelConfiguration(new CompositeIdentity("ollama", "test"), ContextLength: 1_000, TokenLimit: 100);

        await compactor.CompactAsync(BuildAgentContext(prompt, config), prompt, model, config, force: false, CancellationToken.None);

        await Assert.That(capturedPrompts.Count).IsEqualTo(1);
        var sent = capturedPrompts[0];
        await Assert.That(sent.Turns[0].Role).IsEqualTo(ModelRole.System);
        await Assert.That(sent.Turns[0].Content).IsEqualTo(prompt.Turns[0].Content);
        await Assert.That(sent.Turns[1].Role).IsEqualTo(ModelRole.System);
        await Assert.That(sent.Turns[1].Content).IsEqualTo("compaction-system");
        var originalUserContent = prompt.Turns[^1].Content;
        await Assert.That(sent.Turns).DoesNotContain(t => t.Content == originalUserContent);
    }

    private static ContextCompactor BuildCompactor()
    {
        var provider = Substitute.For<IAgentContextProvider>();
        var store = Substitute.For<IContextStore>();
        var liveContext = Substitute.For<IAgentContext>();
        store.OpenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(liveContext));
        var publisher = Substitute.For<IEventPublisher>();
        var compactorCurrentAgent = new CurrentAgentAccessor();
        var dataContextFactory = TestAgentConfigs.DataContextFactoryWith(TestAgentConfigs.WithHeartbeat(TimeSpan.Zero, "test"));
        var runner = new InferenceRunner(
            publisher,
            Substitute.For<IToolCallDispatcher>(),
            TimeProvider.System,
            Substitute.For<IPromptContextProvider>(),
            Substitute.For<IMemorySearcher>(),
            dataContextFactory,
            NullLogger<InferenceRunner>.Instance);
        var systemPrompt = Substitute.For<ISystemPromptProvider>();
        systemPrompt.GetAsync(Arg.Any<string?>(), Arg.Any<IReadOnlyDictionary<string, object?>>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult("compaction-system"));
        var serverRegistry = Substitute.For<IModelContextProtocolServerRegistry>();
        serverRegistry.Resolve(Arg.Any<ImmutableHashSet<string>?>())
            .Returns(new Dictionary<string, ModelContextProtocolServerOptions>(StringComparer.OrdinalIgnoreCase));
        var toolDiscovery = Substitute.For<IModelContextProtocolToolDiscovery>();
        toolDiscovery.DiscoverAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(ImmutableArray<ToolGroup>.Empty));
        var currentAgent = compactorCurrentAgent;
        var locator = Substitute.For<ITemplateFileLocator>();
        locator.Locate(Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<string>()).Returns("/tmp/llamashears-test/PROMPT.md");
        var templateRenderer = Substitute.For<ITemplateRenderer>();
        templateRenderer.RenderAsync(Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, object?>>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<string?>("compaction-kicker"));
        return new ContextCompactor(provider, store, runner, publisher, systemPrompt, serverRegistry, toolDiscovery, currentAgent, locator, templateRenderer, dataContextFactory, NullLogger<ContextCompactor>.Instance);
    }

    private static AgentContext BuildAgentContext(ModelPrompt prompt, ModelConfiguration config)
    {
        var totalEstimate = 0;
        foreach (var turn in prompt.Turns)
        {
            totalEstimate += (int)Math.Ceiling(turn.Content.Length * 1.5 / 2.0);
        }
        var agentConfig = new AgentConfig(
            Model: config,
            ModelContextProtocolServers: []);
        return new AgentContext(
            AgentId: "test",
            Now: _now,
            Config: agentConfig,
            LanguageModel: new LanguageModelContext(
                Turns: [.. prompt.Turns],
                Entries: [.. prompt.Turns.Cast<IContextEntry>()],
                ContextWindowTokenCount: totalEstimate),
            System: new SystemContext(),
            Tools: new ToolContext([]),
            Plugins: new PluginContext([]));
    }

    private static ILanguageModel BuildModel(string summary = "")
    {
        var model = Substitute.For<ILanguageModel>();
        StubEstimate(model);
        model.PromptAsync(Arg.Any<ModelPrompt>(), Arg.Any<PromptOptions?>(), Arg.Any<CancellationToken>())
            .Returns(_ => AsyncEnum<IModelResponseFragment>(new TextFragment(summary)));
        return model;
    }

    private static void StubEstimate(ILanguageModel model)
    {
        model.EstimateAsync(Arg.Any<ModelTurn>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var turn = call.Arg<ModelTurn>();
                return ValueTask.FromResult((int)Math.Ceiling(turn.Content.Length * 1.5 / 2.0));
            });
    }

    private static async IAsyncEnumerable<T> AsyncEnum<T>(params T[] items)
    {
        foreach (var item in items)
        {
            yield return item;
            await Task.Yield();
        }
    }

    private static ModelPrompt ShortPromptWithFiveTurns() =>
        new ModelPrompt([
            new ModelTurn(ModelRole.System, "you are a helpful agent", _now), new ModelTurn(ModelRole.User, "hi", _now),
            new ModelTurn(ModelRole.Assistant, "hi back", _now), new ModelTurn(ModelRole.User, "ping", _now),
            new ModelTurn(ModelRole.Assistant, "pong", _now), new ModelTurn(ModelRole.User, "what's up", _now),
        ]);

    private static ModelPrompt LongPromptOver(int charsPerTurn)
    {
        var filler = new string('x', charsPerTurn);
        return new ModelPrompt([
            new ModelTurn(ModelRole.System, "you are a helpful agent", _now),
            new ModelTurn(ModelRole.User, filler, _now),
            new ModelTurn(ModelRole.Assistant, filler, _now),
            new ModelTurn(ModelRole.User, filler, _now),
            new ModelTurn(ModelRole.Assistant, filler, _now),
            new ModelTurn(ModelRole.User, "the latest user message", _now),
        ]);
    }

    private sealed record TextFragment(string Content) : IModelTextResponse;
}
