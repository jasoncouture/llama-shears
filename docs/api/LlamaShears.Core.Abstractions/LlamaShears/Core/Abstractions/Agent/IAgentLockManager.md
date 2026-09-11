# LlamaShears.Core.Abstractions.Agent.IAgentLockManager

Assembly: `LlamaShears.Core.Abstractions`

Hands out exclusive locks keyed by session canonical id
(`agentId:guid:name`). Each key is backed by a dedicated
single-permit semaphore, so concurrent acquires for the same
session serialize; acquires for different sessions proceed
independently (including two sessions of the same agent).
Implementations are expected to be singleton.

## Methods

### `AcquireLockAsync`(string key, CancellationToken cancellationToken)

Acquires the lock for `key` (a session canonical
id), waiting until any prior holder releases or until
`cancellationToken` fires. Returns a scope
whose disposal releases the lock.

