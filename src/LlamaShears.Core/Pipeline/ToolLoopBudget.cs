using System.Collections.Immutable;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Pipeline;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Core.Pipeline;

public sealed class ToolLoopBudget : IToolLoopBudget
{
    private readonly IDataContextScope _dataScope;
    private int _iteration;

    public ToolLoopBudget(IDataContextScope dataScope)
    {
        _dataScope = dataScope;
    }

    /// <inheritdoc />
    public int Iteration => _iteration;

    /// <inheritdoc />
    public bool IsFinal
    {
        get
        {
            var limit = _dataScope.GetAgentConfig().Tools.TurnLimit;
            return limit > 0 && _iteration >= limit;
        }
    }

    /// <inheritdoc />
    public void ObserveBatch(ImmutableArray<ModelTurn> batch)
    {
        if (!batch.IsDefaultOrEmpty
            && batch.Any(turn => turn.Role is ModelRole.User or ModelRole.FrameworkUser))
        {
            _iteration = 0;
        }

        _iteration++;
    }
}
