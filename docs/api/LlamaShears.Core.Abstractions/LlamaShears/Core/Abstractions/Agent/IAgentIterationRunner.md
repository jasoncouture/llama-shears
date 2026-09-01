# LlamaShears.Core.Abstractions.Agent.IAgentIterationRunner

Assembly: `LlamaShears.Core.Abstractions`

Runs a single agent iteration from the pipeline bag: takes
[AgentPipelineContext](Pipeline/AgentPipelineContext.md).`Prompt` (built by compaction
middleware), inserts [AgentPipelineContext](Pipeline/AgentPipelineContext.md).`EphemeralContext`
once, invokes the language model (with the empty-response retry),
persists the model's output via the active context store, and
returns any tool-result turns the caller should feed back on the
next iteration. Knows nothing about session queues, agent locks,
or interrupt subscriptions.

## Methods

### `RunAsync`([AgentPipelineContext](Pipeline/AgentPipelineContext.md) context)

Runs one iteration from `context`. The caller
is responsible for any lock acquisition, interrupt-token
wiring, and acting on returned tool-result turns.

#### Parameters

- `context` — The per-batch bag. Reads [AgentPipelineContext](Pipeline/AgentPipelineContext.md).`AgentContext`,
[AgentPipelineContext](Pipeline/AgentPipelineContext.md).`Batch`,
[AgentPipelineContext](Pipeline/AgentPipelineContext.md).`CorrelationId`,
[AgentPipelineContext](Pipeline/AgentPipelineContext.md).`TurnToken`,
[AgentPipelineContext](Pipeline/AgentPipelineContext.md).`Prompt`, and
[AgentPipelineContext](Pipeline/AgentPipelineContext.md).`EphemeralContext`.
The run-iteration middleware stores the returned
[IterationOutcome](IterationOutcome.md) on the bag.

