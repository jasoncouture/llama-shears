# LlamaShears.Core.Abstractions.Context.AgentContext

Assembly: `LlamaShears.Core.Abstractions`

Composed snapshot of everything a tool, compactor, or plugin needs
to know about "the current agent right now". Built on demand by
[IAgentContextProvider](IAgentContextProvider.md); components consume slices
([AgentContext](AgentContext.md).`LanguageModel`, [AgentContext](AgentContext.md).`Tools`, etc.) rather
than reaching back into the host's services directly.

## Parameters

- `AgentId` — Identifier of the agent the snapshot is built for.
- `Now` — Wall-clock time captured when the snapshot was created.
- `Config` — The agent's loaded configuration snapshot.
- `LanguageModel` — Conversation log slice (turns, raw entries, model context-window size).
- `System` — System-level context (host metadata, etc.).
- `Tools` — Tool catalog visible to the agent for this snapshot.
- `Plugins` — Free-form data bag plugins use to thread state through context.

## Properties

### `AgentId`

Identifier of the agent the snapshot is built for.

### `Config`

The agent's loaded configuration snapshot.

### `LanguageModel`

Conversation log slice (turns, raw entries, model context-window size).

### `Now`

Wall-clock time captured when the snapshot was created.

### `Plugins`

Free-form data bag plugins use to thread state through context.

### `System`

System-level context (host metadata, etc.).

### `Tools`

Tool catalog visible to the agent for this snapshot.

## Methods

### `AgentContext`(string AgentId, DateTimeOffset Now, AgentConfig Config, [LanguageModelContext](LanguageModelContext.md) LanguageModel, [SystemContext](SystemContext.md) System, [ToolContext](ToolContext.md) Tools, [PluginContext](PluginContext.md) Plugins)

Composed snapshot of everything a tool, compactor, or plugin needs
to know about "the current agent right now". Built on demand by
[IAgentContextProvider](IAgentContextProvider.md); components consume slices
([AgentContext](AgentContext.md).`LanguageModel`, [AgentContext](AgentContext.md).`Tools`, etc.) rather
than reaching back into the host's services directly.

#### Parameters

- `AgentId` — Identifier of the agent the snapshot is built for.
- `Now` — Wall-clock time captured when the snapshot was created.
- `Config` — The agent's loaded configuration snapshot.
- `LanguageModel` — Conversation log slice (turns, raw entries, model context-window size).
- `System` — System-level context (host metadata, etc.).
- `Tools` — Tool catalog visible to the agent for this snapshot.
- `Plugins` — Free-form data bag plugins use to thread state through context.

