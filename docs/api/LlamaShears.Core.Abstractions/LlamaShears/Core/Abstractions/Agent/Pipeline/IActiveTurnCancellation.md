# LlamaShears.Core.Abstractions.Agent.Pipeline.IActiveTurnCancellation

Assembly: `LlamaShears.Core.Abstractions`

Scoped slot for the cancellation source of the in-flight turn.
Interrupt-scope middleware registers the linked source for the
batch; inbound interrupt handling calls [IActiveTurnCancellation](IActiveTurnCancellation.md).`Cancel` so
the two do not share a private field on the loop owner.

## Methods

### `Cancel`

Cancels the registered source, if any. No-op when no turn is
in flight. Does not throw if the source is already cancelled.

### `Register`(CancellationTokenSource cancellationTokenSource)

Installs `cancellationTokenSource` as the
current turn's source. Replaces any previously registered
source without cancelling it.

#### Parameters

- `cancellationTokenSource` — The linked source for this batch. Must not be `null`.

### `Unregister`(CancellationTokenSource cancellationTokenSource)

Clears the slot when it still holds `cancellationTokenSource`.
A later batch's source is left alone.

#### Parameters

- `cancellationTokenSource` — The source this caller registered.

