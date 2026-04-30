namespace LlamaShears.Core.Abstractions.Agent;

public record AgentInfo(
    string AgentId,
    string ModelId,
    int ContextWindowSize,
    IReadOnlyDictionary<string, object>? Parameters = null);
