namespace LlamaShears.Core.Abstractions.Events.Agent;

public sealed record AgentMessageFragment(string Content, bool Final);
