namespace LlamaShears.Core;

public sealed class FileParserCacheOptions
{
    public TimeSpan TimeToLive { get; set; } = TimeSpan.FromMinutes(10);
}
