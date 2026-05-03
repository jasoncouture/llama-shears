namespace LlamaShears.Core.Abstractions.Events.Channel
{
    public record ChannelMessage(string Text, DateTimeOffset Timestamp);
}