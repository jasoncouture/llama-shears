using System.Collections.Immutable;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Core.Abstractions.Agent.Pipeline;

/// <summary>
/// Per-session count of model rounds since the last inbound
/// user or framework-user batch. Used with
/// <see cref="AgentToolConfig.TurnLimit"/> (zero = unlimited).
/// </summary>
public interface IToolLoopBudget
{
    /// <summary>
    /// Record an inbound batch. Resets when the batch contains a
    /// <see cref="ModelRole.User"/> or <see cref="ModelRole.FrameworkUser"/>
    /// turn, then increments the iteration counter.
    /// </summary>
    void ObserveBatch(ImmutableArray<ModelTurn> batch);

    /// <summary>1-based count of model rounds in the current budget window.</summary>
    int Iteration { get; }

    /// <summary>
    /// <see langword="true"/> when a positive
    /// <see cref="AgentToolConfig.TurnLimit"/> has been reached
    /// and this iteration must run without tools.
    /// </summary>
    bool IsFinal { get; }
}
