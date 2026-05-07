namespace LlamaShears.Core.Cron;

public sealed class CronOptions
{
    public string FileName { get; set; } = "cron.json";

    public TimeSpan TickInterval { get; set; } = TimeSpan.FromSeconds(30);
}
