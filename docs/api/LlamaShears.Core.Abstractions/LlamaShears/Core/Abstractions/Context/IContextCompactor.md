# LlamaShears.Core.Abstractions.Context.IContextCompactor

Assembly: `LlamaShears.Core.Abstractions`

Decides whether a [ModelPrompt](../Provider/ModelPrompt.md) exceeds the model's
context window and, if so, rewrites it so the next model call
fits — typically by summarizing earlier turns into a single
assistant message and preserving the trailing user turn. Pure
w.r.t. external storage; callers archive any displaced context
themselves.

## Methods

### `CompactAsync`([AgentContext](AgentContext.md) agentContext, [ModelPrompt](../Provider/ModelPrompt.md) prompt, [ILanguageModel](../Provider/ILanguageModel.md) model, [ModelConfiguration](../Provider/ModelConfiguration.md) configuration, bool force, CancellationToken cancellationToken)

Returns `prompt` unchanged when no compaction
is needed (under budget, too few turns, or no context window
known). Otherwise returns a rebuilt prompt; reference equality
with the input is the caller's signal that compaction did or
did not occur. Pass `force` as `true`
to skip the under-budget guard (eager compaction); the
min-turn-count and missing-context-length guards still apply.

