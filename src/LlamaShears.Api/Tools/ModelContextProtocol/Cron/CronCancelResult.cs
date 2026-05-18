using LlamaShears.Core.Tools.ModelContextProtocol;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Cron;

public sealed record CronCancelResult(
    Guid? JobId,
    bool Cancelled,
    string? Error = null) : IToolResponse;
