namespace LlamaShears.Debugging;

public sealed class EventBusFileLogOptions
{
    public bool Enabled { get; set; }
    public string Filename { get; set; } = "events.jsonl";
}
