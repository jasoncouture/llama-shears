# LlamaShears.Core.Abstractions.Agent.Sessions.ISessionFactory

Assembly: `LlamaShears.Core.Abstractions`

Per-agent registry of live sessions. Backed by a concurrent
dictionary keyed by [SessionId](SessionId.md); sessions are created
on first [ISessionFactory](ISessionFactory.md).`Get` via `ActivatorUtilities` and reused
on subsequent requests.



Today the per-session surface is just [ISessionQueue](ISessionQueue.md);
the return type will broaden when the session interface lands.

## Methods

### `DeleteAsync`([SessionId](SessionId.md) sessionId, CancellationToken cancellationToken)

Removes the session identified by `sessionId`
from the registry and disposes it. No-op when the session is
not present. Async because session disposal involves draining a
channel and tearing down a DI scope, both of which are async.

### `Get`([SessionId](SessionId.md) sessionId)

Returns the session for `sessionId`, creating
it if absent (matches Dictionary's indexer).

### `TryGet`([SessionId](SessionId.md) sessionId, [ISessionQueue](ISessionQueue.md)& session)

Returns the existing session for `sessionId`
without creating a new one. `true` when present
(matches Dictionary.`TryGetValue`'s contract).

