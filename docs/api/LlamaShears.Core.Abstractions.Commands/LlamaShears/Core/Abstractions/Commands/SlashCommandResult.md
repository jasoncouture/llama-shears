# LlamaShears.Core.Abstractions.Commands.SlashCommandResult

Assembly: `LlamaShears.Core.Abstractions.Commands`

Outcome of an [ISlashCommand](ISlashCommand.md) execution. The flags here
are post-execution hints to the dispatcher (e.g. the chat UI) so it
can apply downstream effects without the command coupling to UI
concerns directly.

## Parameters

- `ContextChanged` — `true` when the command mutated the agent's
conversation context (cleared, archived, etc.). Hosts that render
the conversation should refresh their view.

## Properties

### `ContextChanged`

`true` when the command mutated the agent's
conversation context (cleared, archived, etc.). Hosts that render
the conversation should refresh their view.

### `ContextWasChanged`

Result signalling that the agent's context was modified.

### `Default`

Result with no post-execution side-effects.

## Methods

### `SlashCommandResult`(bool ContextChanged)

Outcome of an [ISlashCommand](ISlashCommand.md) execution. The flags here
are post-execution hints to the dispatcher (e.g. the chat UI) so it
can apply downstream effects without the command coupling to UI
concerns directly.

#### Parameters

- `ContextChanged` — `true` when the command mutated the agent's
conversation context (cleared, archived, etc.). Hosts that render
the conversation should refresh their view.

