using LlamaShears.Core.Tools.ModelContextProtocol;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Cron;

public sealed record CronScheduleResult(
    bool Scheduled,
    CronJobSummary? Job,
    string? Error = null) : IToolResponse;
