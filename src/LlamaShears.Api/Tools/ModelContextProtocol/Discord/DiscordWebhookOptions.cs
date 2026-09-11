namespace LlamaShears.Api.Tools.ModelContextProtocol.Discord;

public sealed class DiscordWebhookOptions
{
    public const string DefaultConfigurationSection = "Discord";

    public const string HttpClientName = "DiscordWebhook";

    public Dictionary<string, string> Webhooks { get; set; } = [];
}
