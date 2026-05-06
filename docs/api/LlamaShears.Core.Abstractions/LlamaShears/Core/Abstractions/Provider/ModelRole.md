# LlamaShears.Core.Abstractions.Provider.ModelRole

Assembly: `LlamaShears.Core.Abstractions`

Speaker role attached to a [ModelTurn](ModelTurn.md). Distinguishes
genuine user/assistant traffic from framework-injected scaffolding
and from hidden reasoning that must be filtered out before the
turn is sent back to the model.

## Fields

### `Assistant`

An assistant turn produced by the model.

### `FrameworkAssistant`

An assistant turn injected by the framework (e.g. compaction summaries).

### `FrameworkUser`

A user-authored turn injected by the framework (heartbeat, system signals).

### `System`

The model's persistent system prompt (typically one per conversation).

### `SystemEphemeral`

Per-turn ephemeral system context appended to the next user turn rather than persisted as a separate turn.

### `Thought`

Hidden chain-of-thought emitted by a thinking-capable model. Never resubmitted to the model.

### `Tool`

Tool-call result fed back into the conversation.

### `User`

A user-authored turn.

