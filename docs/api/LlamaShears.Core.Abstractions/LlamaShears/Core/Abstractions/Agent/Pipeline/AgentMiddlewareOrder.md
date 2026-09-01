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

### `CorrelationScope`

Assign a correlation id and logger scope.

### `InterruptScope`

Install the linked turn cancellation token.

### `RunIteration`

Invoke [IAgentIterationRunner](../IAgentIterationRunner.md).

### `SystemPrompt`

Render the persistent system-prompt turn onto the bag.

### `ToolResultEnqueue`

Re-enqueue tool-result turns when the iteration was not interrupted.

### `TurnException`

Swallow turn failures; rethrow shutdown cancellation.

