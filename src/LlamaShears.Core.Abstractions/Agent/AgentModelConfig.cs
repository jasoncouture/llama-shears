using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Core.Abstractions.Agent;

public sealed record AgentModelConfig
{
    public required ModelIdentity Id { get; init; }

    public ThinkLevel Think { get; init; } = ThinkLevel.None;

    public int? ContextLength { get; init; }

    public TimeSpan? KeepAlive { get; init; }

    public int TokenLimit { get; init; }
}
