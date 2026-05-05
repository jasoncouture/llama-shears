namespace LlamaShears.Core.Abstractions.Events.Agent;

public sealed record AgentToolCallFragment(
    string Source,
    string Name,
    string ArgumentsJson,
    string? CallId = null);
