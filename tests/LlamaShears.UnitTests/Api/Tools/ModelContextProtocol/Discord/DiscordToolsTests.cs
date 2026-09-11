using System.Net;
using LlamaShears.Api.Tools.ModelContextProtocol.Discord;
using LlamaShears.Api.Tools.ModelContextProtocol.Filesystem;
using LlamaShears.UnitTests.Api.Tools.ModelContextProtocol.Filesystem;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace LlamaShears.UnitTests.Api.Tools.ModelContextProtocol.Discord;

public sealed class DiscordToolsTests
{
    private const string ValidUrl = "https://discord.com/api/webhooks/123456789012345678/secret-token";

    [Test]
    public async Task ListReturnsConfiguredNamesWithoutUrls()
    {
        var tool = CreateTool(new Dictionary<string, string>
        {
            ["zeta"] = ValidUrl,
            ["alerts"] = ValidUrl,
        });

        var result = await tool.ListWebhooks(CancellationToken.None);

        await Assert.That(result.Error).IsNull();
        await Assert.That(result.WebhookCount).IsEqualTo(2);
        await Assert.That(result.Webhooks.ToArray()).IsEquivalentTo(["alerts", "zeta"]);
        await Assert.That(result.Webhooks.Any(name => name.Contains("secret", StringComparison.Ordinal))).IsFalse();
    }

    [Test]
    public async Task ListRefusesWithoutAnAuthenticatedAgent()
    {
        var tool = CreateTool(new Dictionary<string, string> { ["alerts"] = ValidUrl }, agentId: null);

        var result = await tool.ListWebhooks(CancellationToken.None);

        await Assert.That(result.WebhookCount).IsEqualTo(0);
        await Assert.That(result.Error).Contains("authenticated agent");
    }

    [Test]
    public async Task SendPostsContentToTheNamedWebhook()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, """{"id":"msg-1"}""");
        var tool = CreateTool(new Dictionary<string, string> { ["alerts"] = ValidUrl }, handler);

        var result = await tool.SendMessage("hello discord", webhook: "alerts", cancellationToken: CancellationToken.None);

        await Assert.That(result.Error).IsNull();
        await Assert.That(result.Sent).IsTrue();
        await Assert.That(result.Webhook).IsEqualTo("alerts");
        await Assert.That(result.MessageId).IsEqualTo("msg-1");
        await Assert.That(handler.LastRequest).IsNotNull();
        await Assert.That(handler.LastRequest!.Method).IsEqualTo(HttpMethod.Post);
        await Assert.That(handler.LastRequest.RequestUri!.Host).IsEqualTo("discord.com");
        await Assert.That(handler.LastRequest.RequestUri.Query).Contains("wait=true");
        await Assert.That(handler.LastBody).Contains("hello discord");
        await Assert.That(handler.LastBody).Contains("allowed_mentions");
        await Assert.That(handler.LastBody).DoesNotContain("secret-token");
        await Assert.That(result.Error).IsNull();
    }

    [Test]
    public async Task SendUsesTheOnlyWebhookWhenNameIsOmitted()
    {
        var handler = new RecordingHandler(HttpStatusCode.NoContent, "");
        var tool = CreateTool(new Dictionary<string, string> { ["alerts"] = ValidUrl }, handler);

        var result = await tool.SendMessage("hi", webhook: null, cancellationToken: CancellationToken.None);

        await Assert.That(result.Sent).IsTrue();
        await Assert.That(result.Webhook).IsEqualTo("alerts");
        await Assert.That(result.MessageId).IsNull();
    }

    [Test]
    public async Task SendMatchesWebhookNamesCaseInsensitively()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, """{"id":"9"}""");
        var tool = CreateTool(new Dictionary<string, string> { ["Alerts"] = ValidUrl }, handler);

        var result = await tool.SendMessage("hi", webhook: "alerts", cancellationToken: CancellationToken.None);

        await Assert.That(result.Sent).IsTrue();
        await Assert.That(result.Webhook).IsEqualTo("Alerts");
    }

    [Test]
    public async Task SendIncludesOptionalUsername()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, """{"id":"1"}""");
        var tool = CreateTool(new Dictionary<string, string> { ["alerts"] = ValidUrl }, handler);

        var result = await tool.SendMessage("hi", webhook: "alerts", username: "Claudia", cancellationToken: CancellationToken.None);

        await Assert.That(result.Sent).IsTrue();
        await Assert.That(handler.LastBody).Contains("Claudia");
    }

    [Test]
    public async Task SendRefusesWithoutAnAuthenticatedAgent()
    {
        var tool = CreateTool(new Dictionary<string, string> { ["alerts"] = ValidUrl }, agentId: null);

        var result = await tool.SendMessage("hi", webhook: "alerts", cancellationToken: CancellationToken.None);

        await Assert.That(result.Sent).IsFalse();
        await Assert.That(result.Error).Contains("authenticated agent");
    }

    [Test]
    public async Task SendRefusesWhenNoWebhooksAreConfigured()
    {
        var tool = CreateTool([]);

        var result = await tool.SendMessage("hi", cancellationToken: CancellationToken.None);

        await Assert.That(result.Sent).IsFalse();
        await Assert.That(result.Error).Contains("no Discord webhooks");
    }

    [Test]
    public async Task SendRefusesUnknownWebhookAndListsNames()
    {
        var tool = CreateTool(new Dictionary<string, string>
        {
            ["alerts"] = ValidUrl,
            ["general"] = ValidUrl,
        });

        var result = await tool.SendMessage("hi", webhook: "missing", cancellationToken: CancellationToken.None);

        await Assert.That(result.Sent).IsFalse();
        await Assert.That(result.Error).Contains("unknown Discord webhook 'missing'");
        await Assert.That(result.Error).Contains("alerts");
        await Assert.That(result.Error).Contains("general");
        await Assert.That(result.Error).DoesNotContain("secret-token");
    }

    [Test]
    public async Task SendRequiresANameWhenMultipleWebhooksExist()
    {
        var tool = CreateTool(new Dictionary<string, string>
        {
            ["alerts"] = ValidUrl,
            ["general"] = ValidUrl,
        });

        var result = await tool.SendMessage("hi", webhook: null, cancellationToken: CancellationToken.None);

        await Assert.That(result.Sent).IsFalse();
        await Assert.That(result.Error).Contains("webhook is required");
    }

    [Test]
    public async Task SendRefusesEmptyContent()
    {
        var tool = CreateTool(new Dictionary<string, string> { ["alerts"] = ValidUrl });

        var result = await tool.SendMessage("   ", webhook: "alerts", cancellationToken: CancellationToken.None);

        await Assert.That(result.Sent).IsFalse();
        await Assert.That(result.Error).Contains("content is required");
    }

    [Test]
    public async Task SendRefusesContentOverTheDiscordLimit()
    {
        var tool = CreateTool(new Dictionary<string, string> { ["alerts"] = ValidUrl });

        var result = await tool.SendMessage(
            new string('x', DiscordTools.MaxContentLength + 1),
            webhook: "alerts",
            cancellationToken: CancellationToken.None);

        await Assert.That(result.Sent).IsFalse();
        await Assert.That(result.Error).Contains($"{DiscordTools.MaxContentLength}");
    }

    [Test]
    public async Task SendRefusesUsernameOverTheDiscordLimit()
    {
        var tool = CreateTool(new Dictionary<string, string> { ["alerts"] = ValidUrl });

        var result = await tool.SendMessage(
            "hi",
            webhook: "alerts",
            username: new string('n', DiscordTools.MaxUsernameLength + 1),
            cancellationToken: CancellationToken.None);

        await Assert.That(result.Sent).IsFalse();
        await Assert.That(result.Error).Contains("username");
    }

    [Test]
    public async Task SendSurfacesHttpFailuresWithoutTheWebhookUrl()
    {
        var handler = new RecordingHandler(HttpStatusCode.TooManyRequests, "", TimeSpan.FromSeconds(7));
        var tool = CreateTool(new Dictionary<string, string> { ["alerts"] = ValidUrl }, handler);

        var result = await tool.SendMessage("hi", webhook: "alerts", cancellationToken: CancellationToken.None);

        await Assert.That(result.Sent).IsFalse();
        await Assert.That(result.Error).Contains("HTTP 429");
        await Assert.That(result.Error).Contains("retry after 7s");
        await Assert.That(result.Error).DoesNotContain("secret-token");
    }

    [Test]
    public async Task SendRefusesAMisconfiguredWebhookUrlWithoutEchoingIt()
    {
        var tool = CreateTool(new Dictionary<string, string>
        {
            ["alerts"] = "https://example.com/not-a-discord-webhook",
        });

        var result = await tool.SendMessage("hi", webhook: "alerts", cancellationToken: CancellationToken.None);

        await Assert.That(result.Sent).IsFalse();
        await Assert.That(result.Error).Contains("not a valid Discord webhook URL");
        await Assert.That(result.Error).DoesNotContain("example.com");
    }

    [Test]
    public async Task SendSurfacesTransportFailures()
    {
        var handler = new ThrowingHandler();
        var tool = CreateTool(new Dictionary<string, string> { ["alerts"] = ValidUrl }, handler);

        var result = await tool.SendMessage("hi", webhook: "alerts", cancellationToken: CancellationToken.None);

        await Assert.That(result.Sent).IsFalse();
        await Assert.That(result.Error).Contains("Failed to send");
        await Assert.That(result.Error).DoesNotContain("secret-token");
    }

    private static DiscordTools CreateTool(
        Dictionary<string, string> webhooks,
        HttpMessageHandler? handler = null,
        string? agentId = "alice")
    {
        var options = new DiscordWebhookOptions();
        foreach (var (name, url) in webhooks)
        {
            options.Webhooks[name] = url;
        }

        handler ??= new RecordingHandler(HttpStatusCode.NoContent, "");
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(DiscordWebhookOptions.HttpClientName).Returns(new HttpClient(handler));
        return new DiscordTools(
            new StubAgentWorkspaceLocator(new AgentWorkspace(agentId, "/tmp")),
            Options.Create(options),
            factory,
            NullLogger<DiscordTools>.Instance);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _body;
        private readonly TimeSpan? _retryAfter;

        public RecordingHandler(HttpStatusCode status, string body, TimeSpan? retryAfter = null)
        {
            _status = status;
            _body = body;
            _retryAfter = retryAfter;
        }

        public HttpRequestMessage? LastRequest { get; private set; }

        public string? LastBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            LastBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);
            var response = new HttpResponseMessage(_status)
            {
                Content = new StringContent(_body),
            };
            if (_retryAfter is { } delay)
            {
                response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(delay);
            }

            return response;
        }
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            throw new HttpRequestException("connection reset");
        }
    }
}
