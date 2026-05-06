namespace LlamaShears.Core.Abstractions.Agent.Events;

public sealed record UserMessageSubmitted(string AgentId, string Content, DateTimeOffset At);
