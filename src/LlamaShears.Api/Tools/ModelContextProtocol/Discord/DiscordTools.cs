using System.Collections.Immutable;
using System.ComponentModel;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using LlamaShears.Api.Tools.ModelContextProtocol.Filesystem;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Discord;

[McpServerToolType]
public sealed partial class DiscordTools
{
    public const int MaxContentLength = 2000;
    public const int MaxUsernameLength = 80;

    private readonly IAgentWorkspaceLocator _workspace;
    private readonly IOptions<DiscordWebhookOptions> _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DiscordTools> _logger;

    public DiscordTools(
        IAgentWorkspaceLocator workspace,
        IOptions<DiscordWebhookOptions> options,
        IHttpClientFactory httpClientFactory,
        ILogger<DiscordTools> logger)
    {
        _workspace = workspace;
        _options = options;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [McpServerTool(Name = "discord_list", Destructive = false, OpenWorld = false, ReadOnly = true)]
    [Description("Lists the Discord webhook destinations configured on this host. Returns a JSON object with webhookCount plus an array of webhook names. Use a name from this list as the `webhook` argument to `discord_send`. URLs are never returned.")]
    public async Task<DiscordListResult> ListWebhooks(CancellationToken cancellationToken = default)
    {
        var workspace = await _workspace.GetAsync(cancellationToken);
        if (string.IsNullOrEmpty(workspace.AgentId))
        {
            return new DiscordListResult(
                WebhookCount: 0,
                Webhooks: [],
                Error: "Refused: discord_list requires an authenticated agent on the request.");
        }

        var names = GetConfiguredNames();
        return new DiscordListResult(WebhookCount: names.Length, Webhooks: names);
    }

    [McpServerTool(Name = "discord_send", Destructive = false, OpenWorld = true)]
    [Description("Sends a text message to a configured Discord webhook. Pass a webhook name from `discord_list` (omit it when only one webhook is configured). Do not pass a URL. Content is Discord markdown, max 2000 characters. Optional username overrides the webhook's default display name. @everyone / @here / role / user parses are disabled. Returns sent, the webhook name, and Discord's message id when available.")]
    public async Task<DiscordSendResult> SendMessage(
        [Description("Message body posted to Discord. Required. Max 2000 characters.")]
        string content,
        [Description("Configured webhook name from `discord_list`. Required when more than one webhook is configured; omit to use the only configured webhook.")]
        string? webhook = null,
        [Description("Optional display-name override for this message. Max 80 characters.")]
        string? username = null,
        CancellationToken cancellationToken = default)
    {
        var workspace = await _workspace.GetAsync(cancellationToken);
        if (string.IsNullOrEmpty(workspace.AgentId))
        {
            return new DiscordSendResult(
                Sent: false,
                Webhook: webhook,
                Error: "Refused: discord_send requires an authenticated agent on the request.");
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            return new DiscordSendResult(
                Sent: false,
                Webhook: webhook,
                Error: "Refused: content is required.");
        }

        if (content.Length > MaxContentLength)
        {
            return new DiscordSendResult(
                Sent: false,
                Webhook: webhook,
                Error: $"Refused: content is {content.Length} characters; Discord allows at most {MaxContentLength}.");
        }

        if (username is { Length: > MaxUsernameLength })
        {
            return new DiscordSendResult(
                Sent: false,
                Webhook: webhook,
                Error: $"Refused: username is {username.Length} characters; Discord allows at most {MaxUsernameLength}.");
        }

        if (!TryResolveWebhook(webhook, out var webhookName, out var webhookUrl, out var resolveError))
        {
            return new DiscordSendResult(Sent: false, Webhook: webhookName ?? webhook, Error: resolveError);
        }

        if (!DiscordWebhookUrl.TryValidate(webhookUrl, out var uri, out var urlError))
        {
            return new DiscordSendResult(
                Sent: false,
                Webhook: webhookName,
                Error: $"Refused: configured webhook '{webhookName}' is not a valid Discord webhook URL.");
        }

        var payload = new JsonObject
        {
            ["content"] = content,
            ["allowed_mentions"] = new JsonObject
            {
                ["parse"] = new JsonArray(),
            },
        };
        if (!string.IsNullOrWhiteSpace(username))
        {
            payload["username"] = username;
        }

        var executeUri = new UriBuilder(uri) { Query = "wait=true" }.Uri;
        using var request = new HttpRequestMessage(HttpMethod.Post, executeUri)
        {
            Content = new StringContent(payload.ToJsonString(), System.Text.Encoding.UTF8, "application/json"),
        };

        try
        {
            var client = _httpClientFactory.CreateClient(DiscordWebhookOptions.HttpClientName);
            using var response = await client.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var messageId = await ReadMessageIdAsync(response, cancellationToken);
                LogSendSucceeded(workspace.AgentId, webhookName);
                return new DiscordSendResult(Sent: true, Webhook: webhookName, MessageId: messageId);
            }

            LogSendFailed(workspace.AgentId, webhookName, (int)response.StatusCode);
            return new DiscordSendResult(
                Sent: false,
                Webhook: webhookName,
                Error: DescribeHttpFailure(webhookName, response.StatusCode, response.Headers.RetryAfter?.Delta));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            LogSendException(workspace.AgentId, webhookName, ex);
            return new DiscordSendResult(
                Sent: false,
                Webhook: webhookName,
                Error: $"Failed to send Discord webhook '{webhookName}': {ex.Message}");
        }
    }

    private bool TryResolveWebhook(
        string? requested,
        out string webhookName,
        out string webhookUrl,
        out string error)
    {
        webhookName = "";
        webhookUrl = "";
        error = "";
        var webhooks = _options.Value.Webhooks ?? [];
        if (webhooks.Count == 0)
        {
            error = "Refused: no Discord webhooks are configured. Ask an operator to set Discord:Webhooks.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(requested))
        {
            if (webhooks.Count == 1)
            {
                var only = webhooks.Single();
                webhookName = only.Key;
                webhookUrl = only.Value;
                return true;
            }

            error = $"Refused: webhook is required when more than one Discord webhook is configured. Available: {FormatNames(webhooks.Keys)}.";
            return false;
        }

        var match = webhooks.FirstOrDefault(entry =>
            entry.Key.Equals(requested, StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrEmpty(match.Key))
        {
            webhookName = requested;
            error = $"Refused: unknown Discord webhook '{requested}'. Available: {FormatNames(webhooks.Keys)}.";
            return false;
        }

        webhookName = match.Key;
        webhookUrl = match.Value;
        return true;
    }

    private ImmutableArray<string> GetConfiguredNames()
    {
        var webhooks = _options.Value.Webhooks ?? [];
        return [.. webhooks.Keys.Order(StringComparer.Ordinal)];
    }

    private static string FormatNames(IEnumerable<string> names)
    {
        return string.Join(", ", names.Order(StringComparer.Ordinal));
    }

    private static async Task<string?> ReadMessageIdAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            return null;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            return document.RootElement.TryGetProperty("id", out var id)
                ? id.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string DescribeHttpFailure(string webhookName, HttpStatusCode statusCode, TimeSpan? retryAfter)
    {
        var status = (int)statusCode;
        if (statusCode == HttpStatusCode.TooManyRequests && retryAfter is { } delay)
        {
            return $"Discord rejected webhook '{webhookName}' with HTTP {status} (retry after {delay.TotalSeconds:0}s).";
        }

        return $"Discord rejected webhook '{webhookName}' with HTTP {status}.";
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent '{AgentId}' sent Discord webhook '{WebhookName}'.")]
    private partial void LogSendSucceeded(string agentId, string webhookName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Agent '{AgentId}' Discord webhook '{WebhookName}' failed with HTTP {StatusCode}.")]
    private partial void LogSendFailed(string agentId, string webhookName, int statusCode);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Agent '{AgentId}' Discord webhook '{WebhookName}' failed.")]
    private partial void LogSendException(string agentId, string webhookName, Exception exception);
}
