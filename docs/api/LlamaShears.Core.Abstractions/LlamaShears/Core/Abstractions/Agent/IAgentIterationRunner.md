# LlamaShears.Core.Abstractions.Agent.IAgentIterationRunner

Assembly: `LlamaShears.Core.Abstractions`

Runs a single agent iteration from the pipeline bag: builds the
prompt (including [AgentPipelineContext](Pipeline/AgentPipelineContext.md).`SystemPrompt`
and [AgentPipelineContext](Pipeline/AgentPipelineContext.md).`EphemeralContext`),
invokes the language model (with the empty-response retry),
persists the model's output via the active context store, and
returns any tool-result turns the caller should feed back on the
next iteration. Compaction belongs to the surrounding onion, after
this call returns. Knows nothing about session queues, agent locks,
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
[AgentPipelineContext](Pipeline/AgentPipelineContext.md).`ShutdownToken`,
[AgentPipelineContext](Pipeline/AgentPipelineContext.md).`TurnToken`,
[AgentPipelineContext](Pipeline/AgentPipelineContext.md).`SystemPrompt`, and
[AgentPipelineContext](Pipeline/AgentPipelineContext.md).`EphemeralContext`.
The run-iteration middleware stores the returned
[IterationOutcome](IterationOutcome.md) on the bag.

