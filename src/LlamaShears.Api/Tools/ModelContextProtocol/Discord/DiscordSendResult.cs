using LlamaShears.Core.Tools.ModelContextProtocol;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Discord;

public sealed record DiscordSendResult(
    bool Sent,
    string? Webhook,
    string? MessageId = null,
    string? Error = null) : IToolResponse;
