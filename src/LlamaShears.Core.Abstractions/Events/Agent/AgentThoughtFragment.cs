namespace LlamaShears.Core.Abstractions.Events.Agent;

public sealed record AgentThoughtFragment(string Text, bool Final) : AgentMessageBase(Text);
