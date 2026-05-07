namespace LlamaShears.Core.Cron;

public sealed record CronJobEdit(
    string? Name = null,
    string? CronExpression = null,
    string? Prompt = null,
    bool? Enabled = null);
