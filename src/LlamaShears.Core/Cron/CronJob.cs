namespace LlamaShears.Core.Cron;

public sealed record CronJob(
    Guid Id,
    string AgentId,
    string Name,
    string CronExpression,
    string Prompt,
    DateTimeOffset CreatedAt)
{
    public bool Enabled { get; init; } = true;

    public bool OneShot { get; init; }

    public DateTimeOffset? LastFiredAt { get; init; }

    public DateTimeOffset? NextFireAt { get; init; }
}
