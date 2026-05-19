using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Core.Provider;
using LlamaShears.Provider.OpenAI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace LlamaShears.UnitTests.Provider.OpenAI;

public sealed class OpenAiLanguageModelTests
{
    [Test]
    public async Task PromptAsyncBuildsChatCompletionsRequestWithStreamingAndModel()
    {
        var captured = await CaptureRequestAsync(
            options: new PromptOptions(),
            sseBody: "data: [DONE]\n\n");

        await Assert.That(captured.Method.Method).IsEqualTo("POST");
        await Assert.That(captured.Path).IsEqualTo("/v1/chat/completions");
        var body = JsonNode.Parse(captured.Body)!.AsObject();
        await Assert.That(body["model"]!.GetValue<string>()).IsEqualTo("test-model");
        await Assert.That(body["stream"]!.GetValue<bool>()).IsTrue();
        await Assert.That(body["messages"]).IsNotNull();
    }

    [Test]
    public async Task PromptAsyncMergesExtraRequestParamsIntoBody()
    {
        var hostOptions = new OpenAiProviderOptions();
        hostOptions.ExtraRequestParams["cache_prompt"] = true;
        hostOptions.ExtraRequestParams["slot_id"] = 3;
        hostOptions.ExtraRequestParams["samplers"] = new JsonArray("top_k", "top_p", "min_p", "temperature");

        var captured = await CaptureRequestAsync(
            options: new PromptOptions(),
            sseBody: "data: [DONE]\n\n",
            hostOptions: hostOptions);

        var body = JsonNode.Parse(captured.Body)!.AsObject();
        await Assert.That(body["cache_prompt"]!.GetValue<bool>()).IsTrue();
        await Assert.That(body["slot_id"]!.GetValue<int>()).IsEqualTo(3);
        await Assert.That(body["samplers"]!.AsArray().Count).IsEqualTo(4);
    }

    [Test]
    public async Task PromptAsyncFlattensToolGroupsToSourceDoubleUnderscoreName()
    {
        var captured = await CaptureRequestAsync(
            options: new PromptOptions(Tools:
            [
                new ToolGroup("github", [new ToolDescriptor("create_issue", "", [])]),
                new ToolGroup("linear", [new ToolDescriptor("search", "", [])]),
            ]),
            sseBody: "data: [DONE]\n\n");

        var body = JsonNode.Parse(captured.Body)!.AsObject();
        var tools = body["tools"]!.AsArray();
        var names = tools
            .Select(t => t!["function"]!["name"]!.GetValue<string>())
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

        await Assert.That(names).IsEquivalentTo(["github__create_issue", "linear__search"]);
    }

    [Test]
    public async Task PromptAsyncStreamsTextFragmentsFromContentDeltas()
    {
        var sse =
            "data: {\"choices\":[{\"delta\":{\"content\":\"Hello\"}}]}\n\n" +
            "data: {\"choices\":[{\"delta\":{\"content\":\" world\"}}]}\n\n" +
            "data: [DONE]\n\n";

        var fragments = await CollectFragmentsAsync(new PromptOptions(), sse);

        var text = fragments.OfType<IModelTextResponse>().Select(f => f.Content).ToArray();
        await Assert.That(text).IsEquivalentTo(["Hello", " world"]);
    }

    [Test]
    public async Task PromptAsyncRoutesReasoningContentToThoughtFragments()
    {
        var sse =
            "data: {\"choices\":[{\"delta\":{\"reasoning_content\":\"thinking...\"}}]}\n\n" +
            "data: {\"choices\":[{\"delta\":{\"content\":\"answer\"}}]}\n\n" +
            "data: [DONE]\n\n";

        var fragments = await CollectFragmentsAsync(new PromptOptions(), sse);

        var thoughts = fragments.OfType<IModelThoughtResponse>().Select(f => f.Content).ToArray();
        var text = fragments.OfType<IModelTextResponse>().Select(f => f.Content).ToArray();
        await Assert.That(thoughts).IsEquivalentTo(["thinking..."]);
        await Assert.That(text).IsEquivalentTo(["answer"]);
    }

    [Test]
    public async Task PromptAsyncAccumulatesStreamingToolCallDeltas()
    {
        var sse =
            "data: {\"choices\":[{\"delta\":{\"tool_calls\":[{\"index\":0,\"id\":\"call_1\",\"type\":\"function\",\"function\":{\"name\":\"svc__do\",\"arguments\":\"{\\\"q\\\":\"}}]}}]}\n\n" +
            "data: {\"choices\":[{\"delta\":{\"tool_calls\":[{\"index\":0,\"function\":{\"arguments\":\"\\\"hi\\\"}\"}}]}}]}\n\n" +
            "data: {\"choices\":[{\"finish_reason\":\"tool_calls\"}]}\n\n" +
            "data: [DONE]\n\n";

        var fragments = await CollectFragmentsAsync(new PromptOptions(), sse);

        var toolCall = fragments.OfType<IModelToolCallFragment>().Single().Call;
        await Assert.That(toolCall.Source).IsEqualTo("svc");
        await Assert.That(toolCall.Name).IsEqualTo("do");
        await Assert.That(toolCall.ArgumentsJson).IsEqualTo("{\"q\":\"hi\"}");
        await Assert.That(toolCall.CallId).IsEqualTo("call_1");
    }

    [Test]
    public async Task PromptAsyncEmitsCompletionFragmentFromUsageBlock()
    {
        var sse =
            "data: {\"choices\":[{\"delta\":{\"content\":\"hi\"}}]}\n\n" +
            "data: {\"choices\":[],\"usage\":{\"prompt_tokens\":12,\"completion_tokens\":3}}\n\n" +
            "data: [DONE]\n\n";

        var fragments = await CollectFragmentsAsync(new PromptOptions(), sse);

        var completion = fragments.OfType<IModelCompletionResponse>().Single();
        await Assert.That(completion.TokenCount).IsEqualTo(15);
    }

    [Test]
    public async Task PromptAsyncFoldsThoughtIntoFollowingAssistantReasoningContent()
    {
        var prompt = new ModelPrompt(
        [
            new ModelTurn(ModelRole.User, "ask", DateTimeOffset.UnixEpoch),
            new ModelTurn(ModelRole.Thought, "thinking out loud", DateTimeOffset.UnixEpoch),
            new ModelTurn(ModelRole.Assistant, "answer", DateTimeOffset.UnixEpoch),
            new ModelTurn(ModelRole.User, "follow-up", DateTimeOffset.UnixEpoch),
        ]);

        var captured = await CaptureRequestWithPromptAsync(prompt, new PromptOptions(), "data: [DONE]\n\n");

        var messages = JsonNode.Parse(captured.Body)!["messages"]!.AsArray();
        await Assert.That(messages.Count).IsEqualTo(3);
        await Assert.That(messages[0]!["role"]!.GetValue<string>()).IsEqualTo("user");
        await Assert.That(messages[0]!["content"]!.GetValue<string>()).Contains("ask");
        await Assert.That(messages[1]!["role"]!.GetValue<string>()).IsEqualTo("assistant");
        await Assert.That(messages[1]!["content"]!.GetValue<string>()).IsEqualTo("answer");
        await Assert.That(messages[1]!["reasoning_content"]!.GetValue<string>()).IsEqualTo("thinking out loud");
        await Assert.That(messages[2]!["role"]!.GetValue<string>()).IsEqualTo("user");
        await Assert.That(messages[2]!["content"]!.GetValue<string>()).Contains("follow-up");
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

        var captured = await CaptureRequestWithPromptAsync(prompt, new PromptOptions(), "data: [DONE]\n\n");

        var messages = JsonNode.Parse(captured.Body)!["messages"]!.AsArray();
        await Assert.That(messages.Count).IsEqualTo(2);
        foreach (var message in messages)
        {
            await Assert.That(message!["role"]!.GetValue<string>()).IsNotEqualTo("assistant");
            await Assert.That(message!.AsObject().ContainsKey("reasoning_content")).IsFalse();
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

        var captured = await CaptureRequestWithPromptAsync(prompt, new PromptOptions(), "data: [DONE]\n\n");

        var messages = JsonNode.Parse(captured.Body)!["messages"]!.AsArray();
        await Assert.That(messages.Count).IsEqualTo(2);
        await Assert.That(messages[1]!["role"]!.GetValue<string>()).IsEqualTo("assistant");
        await Assert.That(messages[1]!["reasoning_content"]!.GetValue<string>()).IsEqualTo("first\nsecond");
    }

    [Test]
    public async Task PromptAsyncDropsThoughtImmediatelyBeforeToolTurn()
    {
        var prompt = new ModelPrompt(
        [
            new ModelTurn(ModelRole.User, "q", DateTimeOffset.UnixEpoch),
            new ModelTurn(ModelRole.Thought, "should not survive", DateTimeOffset.UnixEpoch),
            new ModelTurn(ModelRole.Tool, "tool result", DateTimeOffset.UnixEpoch)
            {
                ToolCall = new ToolCall("svc", "do", "{}", "call_1"),
            },
        ]);

        var captured = await CaptureRequestWithPromptAsync(prompt, new PromptOptions(), "data: [DONE]\n\n");

        var messages = JsonNode.Parse(captured.Body)!["messages"]!.AsArray();
        await Assert.That(messages.Count).IsEqualTo(2);
        await Assert.That(messages[1]!["role"]!.GetValue<string>()).IsEqualTo("tool");
        foreach (var message in messages)
        {
            await Assert.That(message!.AsObject().ContainsKey("reasoning_content")).IsFalse();
        }
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

        var captured = await CaptureRequestWithPromptAsync(prompt, new PromptOptions(), "data: [DONE]\n\n");

        var messages = JsonNode.Parse(captured.Body)!["messages"]!.AsArray();
        await Assert.That(messages.Count).IsEqualTo(2);
        await Assert.That(messages[1]!["role"]!.GetValue<string>()).IsEqualTo("assistant");
        foreach (var message in messages)
        {
            await Assert.That(message!.AsObject().ContainsKey("reasoning_content")).IsFalse();
        }
    }

    [Test]
    public async Task PromptAsyncAttachesReasoningAlongsideToolCallsOnAssistantTurn()
    {
        var prompt = new ModelPrompt(
        [
            new ModelTurn(ModelRole.User, "q", DateTimeOffset.UnixEpoch),
            new ModelTurn(ModelRole.Thought, "decide which tool", DateTimeOffset.UnixEpoch),
            new ModelTurn(ModelRole.Assistant, string.Empty, DateTimeOffset.UnixEpoch)
            {
                ToolCalls = [new ToolCall("svc", "do", "{}", "call_1")],
            },
        ]);

        var captured = await CaptureRequestWithPromptAsync(prompt, new PromptOptions(), "data: [DONE]\n\n");

        var messages = JsonNode.Parse(captured.Body)!["messages"]!.AsArray();
        await Assert.That(messages.Count).IsEqualTo(2);
        var assistant = messages[1]!.AsObject();
        await Assert.That(assistant["role"]!.GetValue<string>()).IsEqualTo("assistant");
        await Assert.That(assistant["reasoning_content"]!.GetValue<string>()).IsEqualTo("decide which tool");
        var toolCalls = assistant["tool_calls"]!.AsArray();
        await Assert.That(toolCalls.Count).IsEqualTo(1);
        await Assert.That(toolCalls[0]!["function"]!["name"]!.GetValue<string>()).IsEqualTo("svc__do");
    }

    [Test]
    public async Task PromptAsyncAppliesCustomHeadersFromOptions()
    {
        var hostOptions = new OpenAiProviderOptions
        {
            ApiKey = "key-from-config",
        };
        hostOptions.Headers["X-Org"] = "shears";
        hostOptions.Headers["Authorization"] = "Bearer override-key";

        var captured = await CaptureRequestAsync(
            options: new PromptOptions(),
            sseBody: "data: [DONE]\n\n",
            hostOptions: hostOptions);

        await Assert.That(captured.Headers["X-Org"]).IsEqualTo("shears");
        await Assert.That(captured.Headers["Authorization"]).IsEqualTo("Bearer override-key");
    }

    private sealed record CapturedRequest(
        HttpMethod Method,
        string Path,
        string Body,
        IReadOnlyDictionary<string, string> Headers);

    private static Task<CapturedRequest> CaptureRequestAsync(
        PromptOptions options,
        string sseBody,
        OpenAiProviderOptions? hostOptions = null)
        => CaptureRequestWithPromptAsync(
            new ModelPrompt([new ModelTurn(ModelRole.User, "hi", DateTimeOffset.UnixEpoch)]),
            options,
            sseBody,
            hostOptions);

    private static async Task<CapturedRequest> CaptureRequestWithPromptAsync(
        ModelPrompt prompt,
        PromptOptions options,
        string sseBody,
        OpenAiProviderOptions? hostOptions = null)
    {
        CapturedRequest? captured = null;
        var handler = new StubHandler(async request =>
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var header in request.Headers)
            {
                headers[header.Key] = string.Join(",", header.Value);
            }
            captured = new CapturedRequest(
                request.Method,
                request.RequestUri!.AbsolutePath,
                await request.Content!.ReadAsStringAsync().ConfigureAwait(false),
                headers);
            return BuildSseResponse(sseBody);
        });

        var model = BuildModel(handler, hostOptions);
        await foreach (var _ in model.PromptAsync(prompt, options, CancellationToken.None))
        {
        }

        return captured ?? throw new InvalidOperationException("HTTP request was not sent.");
    }

    private static async Task<List<IModelResponseFragment>> CollectFragmentsAsync(
        PromptOptions options,
        string sseBody)
    {
        var handler = new StubHandler(_ => Task.FromResult(BuildSseResponse(sseBody)));
        var model = BuildModel(handler, hostOptions: null);
        var prompt = new ModelPrompt([new ModelTurn(ModelRole.User, "hi", DateTimeOffset.UnixEpoch)]);
        var collected = new List<IModelResponseFragment>();
        await foreach (var fragment in model.PromptAsync(prompt, options, CancellationToken.None))
        {
            collected.Add(fragment);
        }
        return collected;
    }

    private static OpenAiLanguageModel BuildModel(StubHandler handler, OpenAiProviderOptions? hostOptions)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8080/") };
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient(Arg.Any<string>()).Returns(httpClient);

        var optionsMonitor = Substitute.For<IOptionsMonitor<OpenAiProviderOptions>>();
        optionsMonitor.CurrentValue.Returns(hostOptions ?? new OpenAiProviderOptions
        {
            BaseUri = new Uri("http://localhost:8080"),
        });

        return new OpenAiLanguageModel(
            httpClientFactory,
            optionsMonitor,
            new ModelConfiguration(Id: new CompositeIdentity("openai", "test-model")),
            new ModelTextFormatter(),
            NullLogger<OpenAiLanguageModel>.Instance);
    }

    private static HttpResponseMessage BuildSseResponse(string body) =>
        new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "text/event-stream"),
        };

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

        public StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => _handler(request);
    }
}
