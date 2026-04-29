namespace LlamaShears.Agent.Abstractions.Events;

public sealed record UserMessageSubmitted(string AgentId, string Content, DateTimeOffset At);
