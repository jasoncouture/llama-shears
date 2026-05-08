# LlamaShears.Core.Abstractions.Agent.Sessions.ISessionQueue

Assembly: `LlamaShears.Core.Abstractions.Agent`

Per-session inbound queue for turns the model still needs to see.
Carries two kinds of inputs — user messages arriving from channels,
and tool-result turns produced by dispatched tool calls — and
returns them to the run loop in the order strict providers require:
any pending tool turns first, followed by an optional same-channel
user batch.

## Methods

### `DequeueBatchAsync`(CancellationToken cancellationToken)

Returns the next batch the model should process. Drain order:

- All currently-queued tool turns (non-blocking).
- If any tool turns drained, also drain a same-channel user batch (non-blocking) and append it.
- If no tool turns were available, block until at least one user turn arrives, then drain the same-channel batch.


The returned array is never empty unless the call was cancelled
or the queue has been completed; callers should treat empty as
"shutting down".

### `EnqueueAsync`(ModelTurn turn, CancellationToken cancellationToken)

Appends `turn` to the appropriate internal lane.
User turns batch by ModelTurn.`ChannelId`; tool turns
drain ahead of any pending user batch on the next dequeue.

#### Parameters

- `turn` — The turn to queue. Must have `Role` set to ModelRole.`User` or ModelRole.`Tool`.
- `cancellationToken` — Cancellation for the underlying channel write (typically completes synchronously).

### `HasQueuedMessages`

`true` when at least one tool or user turn is
queued. Useful for callers that want to peek at backlog without
committing to a dequeue.

