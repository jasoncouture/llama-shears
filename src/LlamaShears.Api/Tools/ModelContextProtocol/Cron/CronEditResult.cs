using LlamaShears.Core.Tools.ModelContextProtocol;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Cron;

public sealed record CronEditResult(
    Guid? JobId,
    bool Edited,
    CronJobSummary? Job,
    string? Error = null) : IToolResponse;
