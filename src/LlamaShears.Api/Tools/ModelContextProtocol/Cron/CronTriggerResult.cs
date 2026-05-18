using LlamaShears.Core.Tools.ModelContextProtocol;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Cron;

public sealed record CronTriggerResult(
    Guid? JobId,
    bool Fired,
    string? Error = null) : IToolResponse;
