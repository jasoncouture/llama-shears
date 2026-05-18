using LlamaShears.Core.Tools.ModelContextProtocol;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Session;

public sealed record SessionReplyResult(
    bool Sent,
    string? Error = null) : IToolResponse;
