# LlamaShears.Core.Abstractions.Agent.IAgentLock

Assembly: `LlamaShears.Core.Abstractions`

Scoped passthrough over [IAgentLockManager](IAgentLockManager.md) that
resolves the agent id from the ambient data scope, so callers
don't have to thread the id through every acquire site. Use this
from inside an agent-owned scope; resolve [IAgentLockManager](IAgentLockManager.md)
directly when you need to lock for an arbitrary agent.

## Methods

### `AcquireLockAsync`(CancellationToken cancellationToken)

Acquires the lock for the agent owning the current data scope,
waiting until any prior holder releases or until
`cancellationToken` fires. Returns a scope
whose disposal releases the lock.

