namespace LlamaShears.Core.Abstractions.Events.Agent;

public sealed record AgentToolResultFragment(
    string Source,
    string Name,
    string Result,
    bool IsError = false,
    string? CallId = null);
