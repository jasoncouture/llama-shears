using LlamaShears.Core;
using LlamaShears.Core.Abstractions.Provider;
using NSubstitute;

namespace LlamaShears.UnitTests.Agent.Core;

public sealed class ContextCompactorTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UnixEpoch;

    [Test]
    public async Task BelowMinTurnsReturnsPromptUnchanged()
    {
        var model = BuildModel();
        var compactor = new ContextCompactor();
        var prompt = new ModelPrompt([
            new ModelTurn(ModelRole.System, "you are a helpful agent", Now),
            new ModelTurn(ModelRole.User, "hi", Now),
            new ModelTurn(ModelRole.Assistant, "hi back", Now),
            new ModelTurn(ModelRole.User, "what's up", Now),
        ]);
        var config = new ModelConfiguration("test", ContextLength: 50);

        var result = await compactor.CompactAsync(prompt, model, config, CancellationToken.None);

        await Assert.That(result).IsSameReferenceAs(prompt);
        _ = model.DidNotReceive().PromptAsync(
            Arg.Any<ModelPrompt>(), Arg.Any<PromptOptions?>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task NoContextLengthReturnsPromptUnchanged()
    {
        var model = BuildModel();
        var compactor = new ContextCompactor();
        var prompt = LongPromptOver(charsPerTurn: 10_000);
        var config = new ModelConfiguration("test", ContextLength: null);

        var result = await compactor.CompactAsync(prompt, model, config, CancellationToken.None);

        await Assert.That(result).IsSameReferenceAs(prompt);
        _ = model.DidNotReceive().PromptAsync(
            Arg.Any<ModelPrompt>(), Arg.Any<PromptOptions?>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UnderBudgetReturnsPromptUnchanged()
    {
        var model = BuildModel();
        var compactor = new ContextCompactor();
        var prompt = ShortPromptWithFiveTurns();
        var config = new ModelConfiguration("test", ContextLength: 100_000, TokenLimit: 100);

        var result = await compactor.CompactAsync(prompt, model, config, CancellationToken.None);

        await Assert.That(result).IsSameReferenceAs(prompt);
        _ = model.DidNotReceive().PromptAsync(
            Arg.Any<ModelPrompt>(), Arg.Any<PromptOptions?>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task OverBudgetCompactsToSystemAssistantSummaryAndPreservedUserTurn()
    {
        var model = BuildModel(summary: "here is the summary");
        var compactor = new ContextCompactor();
        var prompt = LongPromptOver(charsPerTurn: 2_000);
        var config = new ModelConfiguration("test", ContextLength: 1_000, TokenLimit: 100);

        var result = await compactor.CompactAsync(prompt, model, config, CancellationToken.None);

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
        var compactor = new ContextCompactor();
        var prompt = LongPromptOver(charsPerTurn: 2_000);
        var config = new ModelConfiguration("test", ContextLength: 900, TokenLimit: 100);

        await compactor.CompactAsync(prompt, model, config, CancellationToken.None);

        // Cap = max(window/3, 256) = max(300, 256) = 300.
        _ = model.Received().PromptAsync(
            Arg.Any<ModelPrompt>(),
            Arg.Is<PromptOptions?>(o => o!.TokenLimit == 300),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SummarizationFloorsAtMinTokenLimit()
    {
        var model = BuildModel(summary: "a summary");
        var compactor = new ContextCompactor();
        var prompt = LongPromptOver(charsPerTurn: 2_000);
        var config = new ModelConfiguration("test", ContextLength: 600, TokenLimit: 100);

        await compactor.CompactAsync(prompt, model, config, CancellationToken.None);

        // Cap = max(600/3, 256) = max(200, 256) = 256.
        _ = model.Received().PromptAsync(
            Arg.Any<ModelPrompt>(),
            Arg.Is<PromptOptions?>(o => o!.TokenLimit == 256),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task EmptySummaryThrowsCompactionFailed()
    {
        var model = BuildModel(summary: "   ");
        var compactor = new ContextCompactor();
        var prompt = LongPromptOver(charsPerTurn: 2_000);
        var config = new ModelConfiguration("test", ContextLength: 1_000, TokenLimit: 100);

        await Assert.That(async () => await compactor.CompactAsync(prompt, model, config, CancellationToken.None))
            .Throws<CompactionFailedException>();
    }

    [Test]
    public async Task SummarizationPromptOmitsTrailingUserTurnAndAppendsInstruction()
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
        var compactor = new ContextCompactor();
        var prompt = LongPromptOver(charsPerTurn: 2_000);
        var config = new ModelConfiguration("test", ContextLength: 1_000, TokenLimit: 100);

        await compactor.CompactAsync(prompt, model, config, CancellationToken.None);

        await Assert.That(capturedPrompts.Count).IsEqualTo(1);
        var sent = capturedPrompts[0];
        await Assert.That(sent.Turns.Count).IsEqualTo(prompt.Turns.Count);
        await Assert.That(sent.Turns[^1].Role).IsEqualTo(ModelRole.User);
        await Assert.That(sent.Turns[^1].Content).Contains("Summarize");
        var originalUserContent = prompt.Turns[^1].Content;
        await Assert.That(sent.Turns).DoesNotContain(t => t.Content == originalUserContent);
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
        // Mirror the default-interface-method formula so the budget math
        // matches what real implementations would yield.
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

    private static ModelPrompt ShortPromptWithFiveTurns() => new([
        new ModelTurn(ModelRole.System, "you are a helpful agent", Now),
        new ModelTurn(ModelRole.User, "hi", Now),
        new ModelTurn(ModelRole.Assistant, "hi back", Now),
        new ModelTurn(ModelRole.User, "ping", Now),
        new ModelTurn(ModelRole.Assistant, "pong", Now),
        new ModelTurn(ModelRole.User, "what's up", Now),
    ]);

    private static ModelPrompt LongPromptOver(int charsPerTurn)
    {
        var filler = new string('x', charsPerTurn);
        return new ModelPrompt([
            new ModelTurn(ModelRole.System, "you are a helpful agent", Now),
            new ModelTurn(ModelRole.User, filler, Now),
            new ModelTurn(ModelRole.Assistant, filler, Now),
            new ModelTurn(ModelRole.User, filler, Now),
            new ModelTurn(ModelRole.Assistant, filler, Now),
            new ModelTurn(ModelRole.User, "the latest user message", Now),
        ]);
    }

    private sealed record TextFragment(string Content) : IModelTextResponse;
}
