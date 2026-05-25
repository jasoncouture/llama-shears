namespace LlamaShears.Api.Tools.ModelContextProtocol.Session;

public sealed record SessionSendResult(string? SessionId, bool Sent, string? Error = null);
