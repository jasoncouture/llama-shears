using System.Runtime.CompilerServices;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Core.Provider;
using LlamaShears.Provider.Ollama;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using NSubstitute;
using OllamaSharp;
using OllamaSharp.Models.Chat;

namespace LlamaShears.UnitTests.Provider.Ollama;

public sealed class OllamaLanguageModelThoughtFoldingTests
{
    [Test]
    public async Task PromptAsyncFoldsThoughtIntoFollowingAssistantThinking()
    {
        var prompt = new ModelPrompt(
        [
            new ModelTurn(ModelRole.User, "ask", DateTimeOffset.UnixEpoch),
            new ModelTurn(ModelRole.Thought, "thinking out loud", DateTimeOffset.UnixEpoch),
            new ModelTurn(ModelRole.Assistant, "answer", DateTimeOffset.UnixEpoch),
            new ModelTurn(ModelRole.User, "follow-up", DateTimeOffset.UnixEpoch),
        ]);

        var captured = await CaptureChatRequestAsync(prompt);

        var messages = captured.Messages!.ToArray();
        await Assert.That(messages.Length).IsEqualTo(3);
        await Assert.That(messages[0].Role).IsEqualTo(ChatRole.User);
        await Assert.That(messages[0].Content).Contains("ask");
        await Assert.That(messages[1].Role).IsEqualTo(ChatRole.Assistant);
        await Assert.That(messages[1].Content).IsEqualTo("answer");
        await Assert.That(messages[1].Thinking).IsEqualTo("thinking out loud");
        await Assert.That(messages[2].Role).IsEqualTo(ChatRole.User);
        await Assert.That(messages[2].Content).Contains("follow-up");
    }

    [Test]
    public async Task PromptAsyncDropsOrphanThoughtNotFollowedByAssistant()
    {
        var prompt = new ModelPrompt(
        [
            new ModelTurn(ModelRole.User, "ask", DateTimeOffset.UnixEpoch),
            new ModelTurn(ModelRole.Thought, "abandoned reasoning", DateTimeOffset.UnixEpoch),
            new ModelTurn(ModelRole.User, "abandon", DateTimeOffset.UnixEpoch),
        ]);

        var captured = await CaptureChatRequestAsync(prompt);

        var messages = captured.Messages!.ToArray();
        await Assert.That(messages.Length).IsEqualTo(2);
        foreach (var message in messages)
        {
            await Assert.That(message.Role).IsNotEqualTo(ChatRole.Assistant);
            await Assert.That(message.Thinking).IsNull();
        }
    }

    [Test]
    public async Task PromptAsyncConcatenatesConsecutiveThoughtsBeforeAssistant()
    {
        var prompt = new ModelPrompt(
        [
            new ModelTurn(ModelRole.User, "q", DateTimeOffset.UnixEpoch),
            new ModelTurn(ModelRole.Thought, "first", DateTimeOffset.UnixEpoch),
            new ModelTurn(ModelRole.Thought, "second", DateTimeOffset.UnixEpoch),
            new ModelTurn(ModelRole.Assistant, "ans", DateTimeOffset.UnixEpoch),
        ]);

        var captured = await CaptureChatRequestAsync(prompt);

        var messages = captured.Messages!.ToArray();
        await Assert.That(messages.Length).IsEqualTo(2);
        await Assert.That(messages[1].Role).IsEqualTo(ChatRole.Assistant);
        await Assert.That(messages[1].Thinking).IsEqualTo("first\nsecond");
    }

    [Test]
    public async Task PromptAsyncDropsTrailingThoughtWithNoFollowingAssistant()
    {
        var prompt = new ModelPrompt(
        [
            new ModelTurn(ModelRole.User, "q", DateTimeOffset.UnixEpoch),
            new ModelTurn(ModelRole.Assistant, "ans", DateTimeOffset.UnixEpoch),
            new ModelTurn(ModelRole.Thought, "tail reasoning", DateTimeOffset.UnixEpoch),
        ]);

        var captured = await CaptureChatRequestAsync(prompt);

        var messages = captured.Messages!.ToArray();
        await Assert.That(messages.Length).IsEqualTo(2);
        await Assert.That(messages[1].Role).IsEqualTo(ChatRole.Assistant);
        foreach (var message in messages)
        {
            await Assert.That(message.Thinking).IsNull();
        }
    }

    private static async Task<ChatRequest> CaptureChatRequestAsync(ModelPrompt prompt)
    {
        var client = Substitute.For<IOllamaApiClient>();
        ChatRequest? captured = null;
        client.ChatAsync(Arg.Do<ChatRequest>(r => captured = r), Arg.Any<CancellationToken>())
            .Returns(EmptyAsync());

        var clientFactory = Substitute.For<IOllamaApiClientFactory>();
        clientFactory.CreateClient(Arg.Any<OllamaProviderOptions>()).Returns(client);
        var hostOptions = Substitute.For<IOptionsMonitor<OllamaProviderOptions>>();
        hostOptions.CurrentValue.Returns(new OllamaProviderOptions());

        var poolProvider = new DefaultObjectPoolProvider();
        var pool = poolProvider.Create<List<Message>>();
        var model = new OllamaLanguageModel(
            clientFactory,
            new ModelConfiguration(Id: new CompositeIdentity("ollama", "test"), Think: ThinkLevel.None),
            hostOptions,
            pool,
            new ModelTextFormatter(),
            NullLogger<OllamaLanguageModel>.Instance);

        await foreach (var _ in model.PromptAsync(prompt, new PromptOptions(), CancellationToken.None))
        {
        }

        return captured ?? throw new InvalidOperationException("ChatAsync was not invoked.");
    }

    private static async IAsyncEnumerable<ChatResponseStream?> EmptyAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        yield break;
    }
}
