# LlamaShears.Core.Abstractions.Events.Event.WellKnown.Agent

Assembly: `LlamaShears.Core.Abstractions.Events`

Agent-level events.

## Properties

### `Busy`

Agent has begun processing a turn.

### `CompactingFinished`

Context compaction has finished.

### `CompactingStarted`

Context compaction has started.

### `Idle`

Agent has finished processing and is idle again.

### `LoadError`

Loading an agent failed.

### `Loaded`

An agent has been loaded into the host.

### `Message`

Streaming user-visible message fragment.

### `Thought`

Streaming hidden-thought fragment.

### `ToolCall`

The agent is dispatching a tool call.

### `ToolResult`

A tool call has produced a result.

### `Turn`

A complete turn has been recorded to the agent's context log.

### `Unloaded`

An agent has been unloaded from the host.

