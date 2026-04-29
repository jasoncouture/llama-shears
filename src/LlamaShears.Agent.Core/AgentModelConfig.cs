using LlamaShears.Provider.Abstractions;

namespace LlamaShears.Agent.Core;

public sealed record AgentModelConfig
{
    public required ModelIdentity Id { get; init; }

    public ThinkLevel Think { get; init; } = ThinkLevel.None;

    public int? ContextLength { get; init; }
}
