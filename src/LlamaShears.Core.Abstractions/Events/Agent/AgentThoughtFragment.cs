namespace LlamaShears.Core.Abstractions.Events.Agent;

public sealed record AgentThoughtFragment(string Content, string? ChannelId = null, bool Final = false)
    : AgentMessageBase(Content, ChannelId);
