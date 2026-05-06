namespace LlamaShears.Core.Abstractions.Events.Agent;

public sealed record AgentMessageFragment(string Text, bool Final) : AgentMessageBase(Text);
