using LlamaShears.Core.Tools.ModelContextProtocol;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Timer;

public sealed record TimerSetResult(
    bool Set,
    Guid? TimerId = null,
    string? SessionId = null,
    int Seconds = 0,
    DateTimeOffset? FiresAt = null,
    string? Prompt = null,
    string? Error = null) : IToolResponse;
