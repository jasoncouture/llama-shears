# LlamaShears.Core.Abstractions.Agent.Pipeline.AgentMiddlewareOrder

Assembly: `LlamaShears.Core.Abstractions`

Well-known [IAgentMiddleware](IAgentMiddleware.md).`Order` values for the
default onion. Lowest is outermost. Built-ins are spaced 1000
apart so a plugin can sit in any gap (or outside the range)
without colliding.

## Fields

### `AgentActivity`

Start and dispose the `chat {model}` activity.

### `AgentLock`

Hold [IAgentLock](../IAgentLock.md) across the rest of the turn.

### `Compaction`

Publish the inbound batch, build and compact `Prompt`.

### `CorrelationScope`

Assign a correlation id and logger scope.

### `EphemeralContext`

Render the ephemeral turn and insert it into `Prompt`.

### `InterruptScope`

Install the linked turn cancellation token.

### `RunIteration`

Invoke [IAgentIterationRunner](../IAgentIterationRunner.md).

### `StripImageAttachments`

Drop image attachments from live context after the model has seen them.

### `SystemPrompt`

Render the persistent system-prompt turn onto the bag.

### `ToolDispatch`

Dispatch `Outcome.ToolCalls` and write `ToolResultTurns`.

### `ToolResultEnqueue`

Re-enqueue tool-result turns when the iteration was not interrupted.

### `TurnException`

Swallow turn failures; rethrow shutdown cancellation.

