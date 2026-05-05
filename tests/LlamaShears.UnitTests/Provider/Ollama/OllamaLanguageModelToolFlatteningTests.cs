using System.Runtime.CompilerServices;
using LlamaShears.Core.Abstractions.Context;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Provider.Ollama;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.ObjectPool;
using NSubstitute;
using OllamaSharp;
using OllamaSharp.Models.Chat;

namespace LlamaShears.UnitTests.Provider.Ollama;

public sealed class OllamaLanguageModelToolFlatteningTests
{
    [Test]
    public async Task PromptAsyncFlattensToolGroupsToSourceDoubleUnderscoreNameOnTheWire()
    {
        var capturedRequest = await CaptureChatRequestAsync(new PromptOptions(
            Tools:
            [
                new ToolGroup("github", [new ToolDescriptor("create_issue", "", [])]),
                new ToolGroup("linear", [new ToolDescriptor("search", "", [])]),
            ]));

        var names = capturedRequest.Tools!
            .OfType<Tool>()
            .Select(t => t.Function!.Name!)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

        await Assert.That(names).IsEquivalentTo(["github__create_issue", "linear__search"]);
    }

    [Test]
    public async Task PromptAsyncOmitsToolsFieldWhenNoGroupsArePresent()
    {
        var captured = await CaptureChatRequestAsync(new PromptOptions());

        await Assert.That(captured.Tools).IsNull();
    }

    [Test]
    public async Task PromptAsyncOmitsToolsFieldWhenAllGroupsAreEmpty()
    {
        var captured = await CaptureChatRequestAsync(new PromptOptions(
            Tools: [new ToolGroup("empty", [])]));

        await Assert.That(captured.Tools).IsNull();
    }

    [Test]
    public async Task PromptAsyncSerializesToolParametersAsJsonSchemaProperties()
    {
        var captured = await CaptureChatRequestAsync(new PromptOptions(
            Tools:
            [
                new ToolGroup("svc",
                [
                    new ToolDescriptor(
                        "do_thing",
                        "does the thing",
                        [
                            new ToolParameter("query", "what to look up", "string", Required: true),
                            new ToolParameter("limit", "max results", "integer", Required: false),
                        ]),
                ]),
            ]));

        var tool = captured.Tools!.OfType<Tool>().Single();
        var fn = tool.Function!;
        await Assert.That(fn.Name).IsEqualTo("svc__do_thing");
        await Assert.That(fn.Description).IsEqualTo("does the thing");

        var parameters = fn.Parameters!;
        await Assert.That(parameters.Type).IsEqualTo("object");
        await Assert.That(parameters.Properties!.Keys).IsEquivalentTo(["query", "limit"]);
        await Assert.That(parameters.Properties!["query"].Type).IsEqualTo("string");
        await Assert.That(parameters.Properties!["limit"].Type).IsEqualTo("integer");
        await Assert.That(parameters.Required).IsEquivalentTo(["query"]);
    }

    private static async Task<ChatRequest> CaptureChatRequestAsync(PromptOptions options)
    {
        var client = Substitute.For<IOllamaApiClient>();
        ChatRequest? captured = null;
        client.ChatAsync(Arg.Do<ChatRequest>(r => captured = r), Arg.Any<CancellationToken>())
            .Returns(EmptyAsync());

        var poolProvider = new DefaultObjectPoolProvider();
        var pool = poolProvider.Create<List<Message>>();
        var model = new OllamaLanguageModel(
            client,
            new ModelConfiguration(ModelId: "test", Think: ThinkLevel.None),
            pool,
            NullLogger<OllamaLanguageModel>.Instance);

        var prompt = new ModelPrompt([new ModelTurn(ModelRole.User, "hi", DateTimeOffset.UnixEpoch)]);

        await foreach (var _ in model.PromptAsync(prompt, options, CancellationToken.None))
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
