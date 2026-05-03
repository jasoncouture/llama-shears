using LlamaShears.Core.Abstractions.Agent.Persistence;
using LlamaShears.Core.Abstractions.Context;
using LlamaShears.Core.Abstractions.Provider;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Core;

public sealed partial class ContextCompactor : IContextCompactor
{
    // 1 system + 2 user + 2 assistant. Below this the trade-off (one model
    // call to produce a summary that wouldn't have been near the limit
    // anyway) isn't worth it; also avoids the pathological case where the
    // summary itself triggers the next compaction.
    private const int MinTurnsForCompaction = 5;

    // Floor for any predict-budget calculation. The compactor never
    // assumes there's less than this many tokens of headroom on either
    // the agent's next response or the summary it requests.
    private const int MinTokenLimitFloor = 256;

    // Default fraction of the context window reserved for the model's
    // response when ModelConfiguration.TokenLimit is unset. Must match
    // the provider's own default reasoning so the budget check stays
    // honest; widen the divisor for larger windows in a follow-up.
    private const int DefaultPredictDivisor = 6;

    // Cap on the summarization call's response. Bounds the rebuilt
    // context so a runaway summary can't itself blow the window.
    private const int SummaryDivisor = 3;

    private const string SummarizeInstruction =
        "This conversation is being compacted to free context. Summarize what came before " +
        "as concise notes — important context, decisions, user goals, and open threads. " +
        "Write it as you would write a note to yourself, since this summary will be your " +
        "only memory of what came before.";
    private readonly IAgentContextProvider _agentContextProvider;
    private readonly IContextStore _contextStore;
    private readonly IInferenceRunner _inferenceRunner;
    private readonly ILogger<ContextCompactor> _logger;

    public ContextCompactor(
        IAgentContextProvider agentContextProvider,
        IContextStore contextStore,
        IInferenceRunner inferenceRunner,
        ILogger<ContextCompactor> logger)
    {
        _agentContextProvider = agentContextProvider;
        _contextStore = contextStore;
        _inferenceRunner = inferenceRunner;
        _logger = logger;
    }

    public async ValueTask<ModelPrompt> CompactAsync(
        AgentContext agentContext,
        ModelPrompt prompt,
        ILanguageModel model,
        ModelConfiguration configuration,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(configuration);

        if (prompt.Turns.Count < MinTurnsForCompaction)
        {
            return prompt;
        }

        if (configuration.ContextLength is not { } window)
        {
            return prompt;
        }

        var totalEstimate = agentContext.LanguageModel.ContextWindowTokenCount;

        var predictBudget = ResolvePredictBudget(configuration, window);
        if (totalEstimate + predictBudget < window)
        {
            return prompt;
        }

        var lastTurn = prompt.Turns[^1];
        if (lastTurn.Role is not (ModelRole.User or ModelRole.FrameworkUser))
        {
            // Trailing turn isn't a user message, so the "preserve last
            // user message, summarize everything else" rebuild doesn't
            // apply. Hand back unchanged; the caller's the one with the
            // policy for "what to do when we can't compact".
            return prompt;
        }

        var summary = await SummarizeAsync(agentContext.AgentId, prompt, model, window, cancellationToken).ConfigureAwait(false);

        var rebuilt = new List<ModelTurn>(3);
        if (prompt.Turns[0].Role == ModelRole.System)
        {
            rebuilt.Add(prompt.Turns[0]);
        }
        rebuilt.Add(new ModelTurn(ModelRole.Assistant, summary, lastTurn.Timestamp));
        rebuilt.Add(lastTurn);
        var rebuiltPrompt = new ModelPrompt(rebuilt);

        // Compaction succeeded: archive the old persisted context and
        // re-seed it with the rebuilt non-system turns. The system turn
        // is reconstructed per-call by the caller and never persisted.
        LogContextCompacted(_logger, agentContext.AgentId);
        await _contextStore.ClearAsync(agentContext.AgentId, archive: true, cancellationToken).ConfigureAwait(false);
        var live = await _contextStore.OpenAsync(agentContext.AgentId, cancellationToken).ConfigureAwait(false);
        foreach (var turn in rebuiltPrompt.Turns)
        {
            if (turn.Role == ModelRole.System)
            {
                continue;
            }
            await live.AppendAsync(turn, cancellationToken).ConfigureAwait(false);
        }
        return rebuiltPrompt;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent '{AgentId}' compacted its context to fit the window.")]
    private static partial void LogContextCompacted(ILogger logger, string agentId);

    private async ValueTask<string> SummarizeAsync(
        string agentId,
        ModelPrompt prompt,
        ILanguageModel model,
        int window,
        CancellationToken cancellationToken)
    {
        // Everything except the trailing user message, plus a fresh
        // user-role instruction asking for the summary. The trailing
        // user turn is preserved out-of-band and re-attached to the
        // rebuilt prompt after summarization completes.
        var historyCount = prompt.Turns.Count - 1;
        var historyTurns = new List<ModelTurn>(historyCount + 1);
        for (var i = 0; i < historyCount; i++)
        {
            historyTurns.Add(prompt.Turns[i]);
        }
        historyTurns.Add(new ModelTurn(ModelRole.User, SummarizeInstruction, prompt.Turns[^1].Timestamp));

        var summarizationPrompt = new ModelPrompt(historyTurns);
        var summaryCap = Math.Max(window / SummaryDivisor, MinTokenLimitFloor);
        var options = new PromptOptions(TokenLimit: summaryCap);

        var outcome = await _inferenceRunner.RunAsync(
            eventId: $"{agentId}-compaction",
            model: model,
            prompt: summarizationPrompt,
            options: options,
            emitTurns: false,
            correlationId: Guid.CreateVersion7(),
            cancellationToken: cancellationToken).ConfigureAwait(false);

        var summary = outcome.Content.Trim();
        if (summary.Length == 0)
        {
            throw new CompactionFailedException(
                "Model produced an empty summary; cannot compact context.");
        }
        return summary;
    }

    private static int ResolvePredictBudget(ModelConfiguration configuration, int window)
    {
        if (configuration.TokenLimit > 0)
        {
            return configuration.TokenLimit;
        }
        return Math.Max(window / DefaultPredictDivisor, MinTokenLimitFloor);
    }
}
