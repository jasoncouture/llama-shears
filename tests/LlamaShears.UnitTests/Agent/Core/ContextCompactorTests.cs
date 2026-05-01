using LlamaShears.Core;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.UnitTests.Agent.Core;

public sealed class ContextCompactorTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UnixEpoch;

    [Test]
    public async Task BelowMinTurnsReturnsPromptUnchanged()
    {
        var compactor = new ContextCompactor();
        var model = ScriptedLanguageModel.WithText("ignored");
        var prompt = new ModelPrompt([
            new ModelTurn(ModelRole.System, "you are a helpful agent", Now),
            new ModelTurn(ModelRole.User, "hi", Now),
            new ModelTurn(ModelRole.Assistant, "hi back", Now),
            new ModelTurn(ModelRole.User, "what's up", Now),
        ]);
        var config = new ModelConfiguration("test", ContextLength: 50);

        var result = await compactor.CompactAsync(prompt, model, config, CancellationToken.None);

        await Assert.That(result).IsSameReferenceAs(prompt);
        await Assert.That(model.PromptInvocations).IsEqualTo(0);
    }

    [Test]
    public async Task NoContextLengthReturnsPromptUnchanged()
    {
        var compactor = new ContextCompactor();
        var model = ScriptedLanguageModel.WithText("ignored");
        var prompt = LongPromptOver(charsPerTurn: 10_000);
        var config = new ModelConfiguration("test", ContextLength: null);

        var result = await compactor.CompactAsync(prompt, model, config, CancellationToken.None);

        await Assert.That(result).IsSameReferenceAs(prompt);
        await Assert.That(model.PromptInvocations).IsEqualTo(0);
    }

    [Test]
    public async Task UnderBudgetReturnsPromptUnchanged()
    {
        var compactor = new ContextCompactor();
        var model = ScriptedLanguageModel.WithText("ignored");
        // Tiny content, huge window — comfortably under budget.
        var prompt = ShortPromptWithFiveTurns();
        var config = new ModelConfiguration("test", ContextLength: 100_000, TokenLimit: 100);

        var result = await compactor.CompactAsync(prompt, model, config, CancellationToken.None);

        await Assert.That(result).IsSameReferenceAs(prompt);
        await Assert.That(model.PromptInvocations).IsEqualTo(0);
    }

    [Test]
    public async Task OverBudgetCompactsToSystemAssistantSummaryAndPreservedUserTurn()
    {
        var compactor = new ContextCompactor();
        var model = ScriptedLanguageModel.WithText("here is the summary");
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
        var compactor = new ContextCompactor();
        var model = ScriptedLanguageModel.WithText("a summary");
        var prompt = LongPromptOver(charsPerTurn: 2_000);
        var config = new ModelConfiguration("test", ContextLength: 900, TokenLimit: 100);

        await compactor.CompactAsync(prompt, model, config, CancellationToken.None);

        // Cap = max(window/3, 256) = max(300, 256) = 300.
        await Assert.That(model.LastOptions).IsNotNull();
        await Assert.That(model.LastOptions!.TokenLimit).IsEqualTo(300);
    }

    [Test]
    public async Task SummarizationFloorsAtMinTokenLimit()
    {
        var compactor = new ContextCompactor();
        var model = ScriptedLanguageModel.WithText("a summary");
        var prompt = LongPromptOver(charsPerTurn: 2_000);
        var config = new ModelConfiguration("test", ContextLength: 600, TokenLimit: 100);

        await compactor.CompactAsync(prompt, model, config, CancellationToken.None);

        // Cap = max(600/3, 256) = max(200, 256) = 256.
        await Assert.That(model.LastOptions!.TokenLimit).IsEqualTo(256);
    }

    [Test]
    public async Task EmptySummaryThrowsCompactionFailed()
    {
        var compactor = new ContextCompactor();
        var model = ScriptedLanguageModel.WithText("   ");
        var prompt = LongPromptOver(charsPerTurn: 2_000);
        var config = new ModelConfiguration("test", ContextLength: 1_000, TokenLimit: 100);

        await Assert.That(async () => await compactor.CompactAsync(prompt, model, config, CancellationToken.None))
            .Throws<CompactionFailedException>();
    }

    [Test]
    public async Task SummarizationPromptOmitsTrailingUserTurnAndAppendsInstruction()
    {
        var compactor = new ContextCompactor();
        var model = ScriptedLanguageModel.WithText("ok");
        var prompt = LongPromptOver(charsPerTurn: 2_000);
        var config = new ModelConfiguration("test", ContextLength: 1_000, TokenLimit: 100);

        await compactor.CompactAsync(prompt, model, config, CancellationToken.None);

        await Assert.That(model.LastPrompt).IsNotNull();
        var sent = model.LastPrompt!;
        // Same count: drop the trailing user, add the summarize instruction.
        await Assert.That(sent.Turns.Count).IsEqualTo(prompt.Turns.Count);
        await Assert.That(sent.Turns[^1].Role).IsEqualTo(ModelRole.User);
        await Assert.That(sent.Turns[^1].Content).Contains("Summarize");
        // The original last user turn must not appear in the summarization prompt.
        var originalUserContent = prompt.Turns[^1].Content;
        await Assert.That(sent.Turns).DoesNotContain(t => t.Content == originalUserContent);
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
}
