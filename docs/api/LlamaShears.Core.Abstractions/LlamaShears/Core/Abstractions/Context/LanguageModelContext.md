# LlamaShears.Core.Abstractions.Context.LanguageModelContext

Assembly: `LlamaShears.Core.Abstractions`

Conversation slice of an [AgentContext](AgentContext.md) snapshot:
chronological turns, the polymorphic entry log they were drawn
from, and the model's current context-window size in tokens.

## Parameters

- `Turns` — Conversation turns in chronological order.
- `Entries` — Polymorphic entry log including non-turn entries (e.g. token-info markers).
- `ContextWindowTokenCount` — The model's context-window size, in tokens.

## Properties

### `ContextWindowTokenCount`

The model's context-window size, in tokens.

### `Entries`

Polymorphic entry log including non-turn entries (e.g. token-info markers).

### `Turns`

Conversation turns in chronological order.

## Methods

### `LanguageModelContext`(ImmutableArray<ModelTurn> Turns, ImmutableArray<IContextEntry> Entries, int ContextWindowTokenCount)

Conversation slice of an [AgentContext](AgentContext.md) snapshot:
chronological turns, the polymorphic entry log they were drawn
from, and the model's current context-window size in tokens.

#### Parameters

- `Turns` — Conversation turns in chronological order.
- `Entries` — Polymorphic entry log including non-turn entries (e.g. token-info markers).
- `ContextWindowTokenCount` — The model's context-window size, in tokens.

