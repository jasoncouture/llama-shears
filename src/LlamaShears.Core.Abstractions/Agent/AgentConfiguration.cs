namespace LlamaShears.Core.Abstractions.Agent;

public record AgentConfiguration(
    string AgentId,
    IReadOnlyDictionary<string, object>? Parameters = null);
