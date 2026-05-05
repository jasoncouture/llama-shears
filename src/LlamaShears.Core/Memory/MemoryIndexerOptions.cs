namespace LlamaShears.Core.Memory;

public sealed class MemoryIndexerOptions
{
    public bool Enabled { get; set; } = true;
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(30);
    public bool ForceOnStartup { get; set; }
}
