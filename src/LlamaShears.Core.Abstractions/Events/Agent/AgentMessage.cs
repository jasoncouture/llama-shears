namespace LlamaShears.Core.Abstractions.Events.Agent;

public sealed record AgentMessage(string Text) : AgentMessageBase(Text);
