# LlamaShears.Core.Abstractions.Agent.Pipeline.AgentMiddlewareDelegate

Assembly: `LlamaShears.Core.Abstractions`

The remainder of the agent-turn onion: either the next
[IAgentMiddleware](IAgentMiddleware.md) or the no-op terminal.
First-registered middleware is outermost and sees the batch before
every later step; last-registered is innermost and sits next to
the terminal.

## Parameters

- `context` — The mutable bag for this dequeued batch.
- `cancellationToken` — Loop-level cancellation (typically [IAgentLifetime](IAgentLifetime.md).`Stopping`).
Per-turn interrupt uses [AgentPipelineContext](AgentPipelineContext.md).`TurnToken`,
not this token.

