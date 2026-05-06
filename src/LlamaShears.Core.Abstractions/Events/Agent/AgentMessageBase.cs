namespace LlamaShears.Core.Abstractions.Events.Agent;

public abstract record AgentMessageBase(string Content, string? ChannelId = null);
