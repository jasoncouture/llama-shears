namespace LlamaShears.Api.Tools.ModelContextProtocol.Cron;

public sealed record CronJobSummary(
    Guid Id,
    string Name,
    string CronExpression,
    string Prompt,
    bool Enabled,
    DateTimeOffset? LastFiredAt,
    DateTimeOffset? NextFireAt);
