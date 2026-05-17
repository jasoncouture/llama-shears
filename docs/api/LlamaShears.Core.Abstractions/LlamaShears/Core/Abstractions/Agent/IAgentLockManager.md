# LlamaShears.Core.Abstractions.Agent.IAgentLockManager

Assembly: `LlamaShears.Core.Abstractions`

Hands out per-agent exclusive locks. Each agent id is backed by a
dedicated single-permit semaphore, so concurrent acquires for the
same agent serialize; acquires for different agents proceed
independently. Implementations are expected to be singleton.

## Methods

### `AcquireLockAsync`(string agentId, CancellationToken cancellationToken)

Acquires the lock for `agentId`, waiting until
any prior holder releases or until `cancellationToken`
fires. Returns a scope whose disposal releases the lock.

