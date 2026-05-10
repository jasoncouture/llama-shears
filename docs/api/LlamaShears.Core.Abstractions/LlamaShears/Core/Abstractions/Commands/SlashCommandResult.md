# LlamaShears.Core.Abstractions.Commands.SlashCommandResult

Assembly: `LlamaShears.Core.Abstractions`

Outcome of an [ISlashCommand](ISlashCommand.md) execution. The flags here
are post-execution hints to the dispatcher (e.g. the chat UI) so it
can apply downstream effects without the command coupling to UI
concerns directly.

## Parameters

- `ContextChanged` — `true` when the command mutated the agent's
conversation context (cleared, archived, etc.). Hosts that render
the conversation should refresh their view.
- `StreamingInterrupted` — `true` when the command stopped an in-flight turn
without changing persisted context. Hosts should close any open
streaming bubbles (assistant message / thought / in-flight tool)
for the active turn so the UI doesn't get stuck waiting for a
final fragment that will never arrive. The persistent
conversation history above the streaming bubble is preserved.

## Properties

### `ContextChanged`

`true` when the command mutated the agent's
conversation context (cleared, archived, etc.). Hosts that render
the conversation should refresh their view.

### `ContextWasChanged`

Result signalling that the agent's context was modified.

### `Default`

Result with no post-execution side-effects.

### `StreamingInterrupted`

`true` when the command stopped an in-flight turn
without changing persisted context. Hosts should close any open
streaming bubbles (assistant message / thought / in-flight tool)
for the active turn so the UI doesn't get stuck waiting for a
final fragment that will never arrive. The persistent
conversation history above the streaming bubble is preserved.

### `StreamingWasInterrupted`

Result signalling that an in-flight turn was interrupted.

## Methods

### `SlashCommandResult`(bool ContextChanged, bool StreamingInterrupted)

Outcome of an [ISlashCommand](ISlashCommand.md) execution. The flags here
are post-execution hints to the dispatcher (e.g. the chat UI) so it
can apply downstream effects without the command coupling to UI
concerns directly.

#### Parameters

- `ContextChanged` — `true` when the command mutated the agent's
conversation context (cleared, archived, etc.). Hosts that render
the conversation should refresh their view.
- `StreamingInterrupted` — `true` when the command stopped an in-flight turn
without changing persisted context. Hosts should close any open
streaming bubbles (assistant message / thought / in-flight tool)
for the active turn so the UI doesn't get stuck waiting for a
final fragment that will never arrive. The persistent
conversation history above the streaming bubble is preserved.

