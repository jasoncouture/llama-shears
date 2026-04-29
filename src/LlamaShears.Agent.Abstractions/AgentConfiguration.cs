namespace LlamaShears.Agent.Abstractions;

public record AgentConfiguration(
    string AgentId,
    IReadOnlyDictionary<string, object>? Parameters = null);
