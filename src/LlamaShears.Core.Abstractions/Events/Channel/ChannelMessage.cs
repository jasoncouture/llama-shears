namespace LlamaShears.Core.Abstractions.Events.Channel
{
    public record ChannelMessage(string Text, string AgentId, DateTimeOffset Timestamp);
}