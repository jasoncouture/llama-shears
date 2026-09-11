using System.Collections.Immutable;
using LlamaShears.Core.Tools.ModelContextProtocol;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Discord;

public sealed record DiscordListResult(
    int WebhookCount,
    ImmutableArray<string> Webhooks,
    string? Error = null) : IToolResponse;
