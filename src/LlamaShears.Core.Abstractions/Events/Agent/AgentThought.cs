namespace LlamaShears.Core.Abstractions.Events.Agent;

public record AgentThought(string Text) : AgentMessageBase(Text);
