# LlamaShears.Core.Abstractions.Agent.IAgent

Assembly: `LlamaShears.Core.Abstractions.Agent`

An autonomous component that ingests input turns, drives a model,
and produces output turns. Identified by [IAgent](IAgent.md).`Id`; the
rest of its surface — heartbeat cadence, channels, conversation
state — is internal and reachable through the services that own
it (config provider, context store, message bus).

## Properties

### `Id`

Stable identifier for this agent.

### `LastActivity`

Timestamp of the most recent turn recorded for this agent —
i.e. the moment of last activity. `null` when
the agent's context has no turns yet. Callers compute idle
duration from this against the current wall clock.

## Methods

### `InterruptAsync`(CancellationToken cancellationToken)

Cancels the in-flight turn (model inference, eager tool dispatch)
without affecting the agent's lifetime. The agent's persisted
context up to the interrupt is preserved; partial assistant text
or thought fragments emitted by the canceled turn are dropped.
Idempotent — calling when no turn is in flight is a no-op.

### `LockAsync`(CancellationToken cancellationToken)

Acquires the agent's processing gate, blocking the run loop
from starting any new batch until [IAgent](IAgent.md).`UnlockAsync`
is called. Pairs 1:1 with [IAgent](IAgent.md).`UnlockAsync`; a caller
that locks must unlock. Backed by a single-permit semaphore.

### `RequestCompactionAsync`(CancellationToken cancellationToken)

Acquires the agent's processing gate and runs the context
compactor against the agent's current context, bypassing the
usual under-budget guard so the call is willing to compact a
healthy-but-aged context. The compactor's other guards (min
turn count, missing context length) still apply, so a small
or unconfigured context is left alone.

### `UnlockAsync`

Releases a permit previously acquired by [IAgent](IAgent.md).`LockAsync`.

