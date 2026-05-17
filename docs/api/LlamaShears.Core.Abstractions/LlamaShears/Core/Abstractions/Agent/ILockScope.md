# LlamaShears.Core.Abstractions.Agent.ILockScope

Assembly: `LlamaShears.Core.Abstractions`

Disposable handle to an acquired agent lock. The scope owns the
underlying semaphore permit; disposing it releases the permit and
flips [ILockScope](ILockScope.md).`Active` to `false`. If the
holder forgets to dispose, the finalizer still releases the permit
so the lock isn't wedged for the rest of the process lifetime.

## Properties

### `Active`

`true` while the scope still holds the
underlying lock; flips to `false` on the first
successful disposal. A finalizer-only release does NOT flip
this flag (the scope is already unreachable at that point), so
callers should never read [ILockScope](ILockScope.md).`Active` after losing
their reference.

