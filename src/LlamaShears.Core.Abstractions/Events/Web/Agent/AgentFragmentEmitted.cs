namespace LlamaShears.Core.Abstractions.Agent.Events;

public sealed record AgentFragmentEmitted(
    string AgentId,
    AgentFragmentKind Kind,
    string Delta,
    Guid StreamId,
    bool IsFinal);
