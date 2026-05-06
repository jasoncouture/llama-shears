namespace LlamaShears.Core.Abstractions.Events.Agent;

public sealed record AgentMessageFragment(string Content, string? ChannelId = null, bool Final = false)
    : AgentMessageBase(Content, ChannelId);
