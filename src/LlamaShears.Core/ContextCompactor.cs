using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Persistence;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.Context;
using LlamaShears.Core.Abstractions.Provider;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Core;

public sealed partial class ContextCompactor : IContextCompactor
{
    private const int PreserveLastTurnCount = 6;
    private const int MinTokenLimitFloor = 256;
    private const int DefaultPredictDivisor = 6;

    private readonly IContextStore _contextStore;
    private readonly CompactionTranscriptFormatter _transcriptFormatter;
    private readonly IDataContextScope _dataContextScope;
    private readonly ILogger<ContextCompactor> _logger;

    public ContextCompactor(
        IContextStore contextStore,
        IDataContextScope dataContextScope,
        ILogger<ContextCompactor> logger)
    {
        _contextStore = contextStore;
        _transcriptFormatter = new CompactionTranscriptFormatter();
        _dataContextScope = dataContextScope;
        _logger = logger;
    }

    public ValueTask<CompactionPlan?> TryPrepareAsync(
        AgentContext agentContext,
        ModelPrompt prompt,
        bool force,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(prompt);

        if (!TrySplit(prompt, out var systemTurn, out var older, out var preserved))
        {
            return ValueTask.FromResult<CompactionPlan?>(null);
        }

        var configuration = _dataContextScope.GetModelConfiguration();
        if (configuration.ContextLength is not { } window)
        {
            return ValueTask.FromResult<CompactionPlan?>(null);
        }

        if (!force)
        {
            var lastReportedTokens = agentContext.LanguageModel.ContextWindowTokenCount;
            var predictBudget = ResolvePredictBudget(configuration, window);
            var nextLength = prompt.Turns[^1].Content?.Length ?? 0;
            var nextTokenEstimate = (nextLength + 1) / 2;
            var threshold = (int)Math.Floor(window * 0.75);
            if (lastReportedTokens + predictBudget + nextTokenEstimate < threshold)
            {
                return ValueTask.FromResult<CompactionPlan?>(null);
            }
        }

        var transcript = new ModelTurn(
            ModelRole.User,
            _transcriptFormatter.Format(older),
            prompt.Turns[^1].Timestamp);
        return ValueTask.FromResult<CompactionPlan?>(
            new CompactionPlan(systemTurn, transcript, [.. preserved]));
    }

    public async ValueTask<ModelPrompt> CommitAsync(
        CompactionPlan plan,
        string summary,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentException.ThrowIfNullOrWhiteSpace(summary);

        var stamp = plan.Preserved.Length > 0 ? plan.Preserved[^1].Timestamp : plan.Transcript.Timestamp;
        var rebuilt = new List<ModelTurn>(plan.Preserved.Length + 2);
        if (plan.SystemTurn is not null)
        {
            rebuilt.Add(plan.SystemTurn);
        }
        rebuilt.Add(new ModelTurn(ModelRole.Assistant, summary, stamp));
        rebuilt.AddRange(plan.Preserved);
        var rebuiltPrompt = new ModelPrompt(rebuilt);

        LogContextCompacted(_dataContextScope.GetAgentConfig().Id);
        var session = _dataContextScope.GetCurrentSessionId();
        await _contextStore.ClearAsync(session, archive: true, cancellationToken);
        var live = await _contextStore.OpenAsync(session, cancellationToken);
        foreach (var turn in rebuiltPrompt.Turns)
        {
            if (turn.Role == ModelRole.System)
            {
                continue;
            }
            await live.AppendAsync(turn, cancellationToken);
        }
        return rebuiltPrompt;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent '{AgentId}' compacted its context to fit the window.")]
    private partial void LogContextCompacted(string agentId);

    private static bool TrySplit(
        ModelPrompt prompt,
        out ModelTurn? systemTurn,
        out IReadOnlyList<ModelTurn> older,
        out IReadOnlyList<ModelTurn> preserved)
    {
        systemTurn = prompt.Turns.Count > 0 && prompt.Turns[0].Role == ModelRole.System
            ? prompt.Turns[0]
            : null;

        List<ModelTurn> eligible = [.. prompt.Turns.Where(IsEligible)];
        if (eligible.Count <= PreserveLastTurnCount)
        {
            older = [];
            preserved = [];
            return false;
        }

        var cut = eligible.Count - PreserveLastTurnCount;
        while (cut > 0 && eligible[cut].Role == ModelRole.Tool)
        {
            cut--;
        }

        if (cut <= 0)
        {
            older = [];
            preserved = [];
            return false;
        }

        older = eligible.GetRange(0, cut);
        preserved = eligible.GetRange(cut, eligible.Count - cut);
        return true;
    }

    private static bool IsEligible(ModelTurn turn)
        => turn.Role is not ModelRole.System and not ModelRole.SystemEphemeral && !turn.Ephemeral;

    private static int ResolvePredictBudget(ModelConfiguration configuration, int window)
    {
        if (configuration.TokenLimit > 0)
        {
            return configuration.TokenLimit;
        }
        return Math.Max(window / DefaultPredictDivisor, MinTokenLimitFloor);
    }
}
