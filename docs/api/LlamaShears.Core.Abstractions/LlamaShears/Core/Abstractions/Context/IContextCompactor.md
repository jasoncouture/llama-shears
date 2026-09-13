# LlamaShears.Core.Abstractions.Context.IContextCompactor

Assembly: `LlamaShears.Core.Abstractions`

Decides whether a [ModelPrompt](../Provider/ModelPrompt.md) needs compaction and
commits a summary plus the preserved tail. Does not call the model;
the pipeline launches a compaction child for the summary.

## Methods

### `CommitAsync`([CompactionPlan](CompactionPlan.md) plan, string summary, CancellationToken cancellationToken)

Archives the live context and writes
`[system?, Assistant(summary), ...preserved]`.

### `TryPrepareAsync`([AgentContext](AgentContext.md) agentContext, [ModelPrompt](../Provider/ModelPrompt.md) prompt, bool force, CancellationToken cancellationToken)

Returns a [CompactionPlan](CompactionPlan.md) when older history should
be summarized, or `null` when no compaction is
needed (under budget, six or fewer eligible turns, or no context
window known). Pass `force` as
`true` to skip the under-budget guard.

