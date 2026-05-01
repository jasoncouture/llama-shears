using System.Text;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Core;

public sealed class ContextCompactor : IContextCompactor
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

    public async ValueTask<ModelPrompt> CompactAsync(
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

        var totalEstimate = 0;
        foreach (var turn in prompt.Turns)
        {
            totalEstimate += await model.EstimateAsync(turn, cancellationToken).ConfigureAwait(false);
        }

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

        var summary = await SummarizeAsync(prompt, model, window, cancellationToken).ConfigureAwait(false);

        var rebuilt = new List<ModelTurn>(3);
        if (prompt.Turns[0].Role == ModelRole.System)
        {
            rebuilt.Add(prompt.Turns[0]);
        }
        rebuilt.Add(new ModelTurn(ModelRole.Assistant, summary, lastTurn.Timestamp));
        rebuilt.Add(lastTurn);
        return new ModelPrompt(rebuilt);
    }

    private async ValueTask<string> SummarizeAsync(
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

        var builder = new StringBuilder();
        await foreach (var fragment in model.PromptAsync(summarizationPrompt, options, cancellationToken).ConfigureAwait(false))
        {
            if (fragment is IModelTextResponse text)
            {
                builder.Append(text.Content);
            }
        }

        var summary = builder.ToString().Trim();
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
