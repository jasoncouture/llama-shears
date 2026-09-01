# LlamaShears.Core.Abstractions.Agent.Pipeline

## Types

- [AgentMiddlewareDelegate](AgentMiddlewareDelegate.md) — The remainder of the agent-turn onion: either the next [IAgentMiddleware](IAgentMiddleware.md) or the no-op terminal. First-registered middleware is outermost and sees the batch before every later step; last-registered is innermost and sits next to the terminal.
- [AgentPipeline](AgentPipeline.md) — Folds [IAgentMiddleware](IAgentMiddleware.md) registrations into an onion. First enumerated step is outermost; last is innermost, wrapping a no-op terminal.
- [AgentPipelineContext](AgentPipelineContext.md) — Mutable bag handed to every [IAgentMiddleware](IAgentMiddleware.md) for one dequeued batch. The loop owner constructs it; middleware fill [AgentPipelineContext](AgentPipelineContext.md).`CorrelationId`, [AgentPipelineContext](AgentPipelineContext.md).`TurnToken`, and [AgentPipelineContext](AgentPipelineContext.md).`Outcome` as they run.
- [AgentPipelineServiceCollectionExtensions](AgentPipelineServiceCollectionExtensions.md) — DI helpers for the per-batch agent middleware onion.
- [IActiveTurnCancellation](IActiveTurnCancellation.md) — Scoped slot for the cancellation source of the in-flight turn. Interrupt-scope middleware registers the linked source for the batch; inbound interrupt handling calls [IActiveTurnCancellation](IActiveTurnCancellation.md).`Cancel` so the two do not share a private field on the loop owner.
- [IAgentLifetime](IAgentLifetime.md) — Per-agent stop signal, analogous to `IHostApplicationLifetime` for one agent scope. The loop watches [IAgentLifetime](IAgentLifetime.md).`Stopping`; inbound shutdown handling and dispose call [IAgentLifetime](IAgentLifetime.md).`Stop`.
- [IAgentMiddleware](IAgentMiddleware.md) — One step in the per-batch agent onion. Implementations do work before and/or after invoking the next delegate; skipping it short-circuits the rest of the chain. Each implementation owns exactly one concern.
- [IAgentPipeline](IAgentPipeline.md) — Invokes the registered [IAgentMiddleware](IAgentMiddleware.md) onion for one dequeued batch. The loop owner builds [AgentPipelineContext](AgentPipelineContext.md) and calls this once per batch; it does not dequeue, lock, or subscribe.

