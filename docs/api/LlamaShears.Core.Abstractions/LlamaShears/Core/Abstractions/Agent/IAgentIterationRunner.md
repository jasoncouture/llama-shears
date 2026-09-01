# LlamaShears.Core.Abstractions.Agent.IAgentIterationRunner

Assembly: `LlamaShears.Core.Abstractions`

Runs a single agent iteration from the pipeline bag: takes
[AgentPipelineContext](Pipeline/AgentPipelineContext.md).`Prompt` (compaction plus
ephemeral insert already applied), invokes the language model
(with the empty-response retry), persists token metrics, and
returns the model's tool calls on [IterationOutcome](IterationOutcome.md).
Dispatch middleware executes those calls. Knows nothing about
session queues, agent locks, or interrupt subscriptions.

## Methods

### `RunAsync`([AgentPipelineContext](Pipeline/AgentPipelineContext.md) context)

Runs one iteration from `context`. The caller
is responsible for any lock acquisition and interrupt-token
wiring. Dispatch middleware acts on returned tool calls.

#### Parameters

- `context` — The per-batch bag. Reads [AgentPipelineContext](Pipeline/AgentPipelineContext.md).`AgentContext`,
[AgentPipelineContext](Pipeline/AgentPipelineContext.md).`Batch`,
[AgentPipelineContext](Pipeline/AgentPipelineContext.md).`CorrelationId`,
[AgentPipelineContext](Pipeline/AgentPipelineContext.md).`TurnToken`,
[AgentPipelineContext](Pipeline/AgentPipelineContext.md).`Prompt`,
[AgentPipelineContext](Pipeline/AgentPipelineContext.md).`SessionId`, and
[AgentPipelineContext](Pipeline/AgentPipelineContext.md).`ChannelId`.
The run-iteration middleware stores the returned
[IterationOutcome](IterationOutcome.md) on the bag.

