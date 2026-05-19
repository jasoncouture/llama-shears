# LlamaShears.Core.Abstractions.Agent.IAgentIterationRunner

Assembly: `LlamaShears.Core.Abstractions`

Runs a single agent iteration: builds the prompt from the supplied
context and turn batch, invokes the language model (with the
empty-response retry), persists the model's output via the active
context store, and returns any tool-result turns the caller should
feed back on the next iteration. Knows nothing about session queues,
agent locks, or interrupt subscriptions — those concerns belong to
the surrounding loop owner.

## Methods

### `RunAsync`([IAgentContext](Persistence/IAgentContext.md) context, ImmutableArray<[ModelTurn](../Provider/ModelTurn.md)> batch, Guid correlationId, CancellationToken outerCancellationToken, CancellationToken turnCancellationToken)

Runs one iteration. The caller is responsible for any lock
acquisition, interrupt-token wiring, and acting on returned
tool-result turns.

#### Parameters

- `context` — Live context for the session being driven. Token usage and any
turn the inference path persists land here.
- `batch` — Input turns for this iteration (typically the freshly dequeued
user/tool turns).
- `correlationId` — Correlation id stamped on every event published during the
iteration. Lets subscribers tie streamed fragments back to the
inbound batch.
- `outerCancellationToken` — Cancellation that should outlive an interrupt — used for tail
persistence work that must finish even after the turn was
interrupted.
- `turnCancellationToken` — Cancellation linked to interrupt signals; cancelling this stops
the inference itself.

