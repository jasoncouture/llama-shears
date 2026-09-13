using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.PromptContext;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Core.Abstractions.SystemPrompt;

namespace LlamaShears.Core.Provider;

public sealed class ModelPromptAssembler : IModelPromptAssembler
{
    private readonly ISystemPromptProvider _systemPrompt;
    private readonly IPromptContextProvider _promptContext;
    private readonly IDataContextScope _dataScope;
    private readonly TimeProvider _time;

    public ModelPromptAssembler(
        ISystemPromptProvider systemPrompt,
        IPromptContextProvider promptContext,
        IDataContextScope dataScope,
        TimeProvider time)
    {
        _systemPrompt = systemPrompt;
        _promptContext = promptContext;
        _dataScope = dataScope;
        _time = time;
    }

    public async ValueTask<ModelPrompt> AssembleAsync(
        IReadOnlyList<ModelTurn> turns,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(turns);

        var config = _dataScope.GetAgentConfig();
        var snapshot = _dataScope.Snapshot();
        var now = _time.GetLocalNow();
        var systemBody = await _systemPrompt.GetAsync(config.SystemPrompt, snapshot, cancellationToken);
        var assembled = new List<ModelTurn>(turns.Count + 2)
        {
            new ModelTurn(ModelRole.System, systemBody, now),
        };
        assembled.AddRange(turns);

        var ephemeralBody = await _promptContext.GetAsync(config.PromptContext, snapshot, cancellationToken);
        if (string.IsNullOrWhiteSpace(ephemeralBody))
        {
            return new ModelPrompt(assembled);
        }

        var ephemeral = new ModelTurn(ModelRole.SystemEphemeral, ephemeralBody, now, Ephemeral: true);
        return new ModelPrompt(InsertEphemeral(assembled, ephemeral));
    }

    private static IReadOnlyList<ModelTurn> InsertEphemeral(IReadOnlyList<ModelTurn> turns, ModelTurn ephemeral)
    {
        if (turns.Count == 0 || turns[^1].Role != ModelRole.User)
        {
            return turns;
        }

        var insertAt = GetLastUserMessageIndex(turns);
        if (insertAt == 0)
        {
            insertAt = turns.Count;
        }

        return [.. turns.Take(insertAt), ephemeral, .. turns.Skip(insertAt)];
    }

    private static int GetLastUserMessageIndex(IEnumerable<ModelTurn> turns)
    {
        return turns.Select((item, index) => (item, index))
            .Reverse()
            .TakeWhile(i => i.item.Role == ModelRole.User)
            .Select(i => i.index)
            .DefaultIfEmpty(0)
            .Last();
    }
}
