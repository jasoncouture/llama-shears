# LlamaShears.Core.Abstractions.Agent.PromptedAgentStartInformation

Assembly: `LlamaShears.Core.Abstractions`

Bundle of inputs needed to launch a prompted sub-agent (heartbeat, cron,
any tick-driven transient). Captures the config, the parent session path,
the initial user-role turn, optional priming turns, and optional data-scope
entries to seed the child's context.

## Parameters

- `Config` — Agent config the child runs under.
- `Id` — Child session id (parent agent id + channel name).
- `ParentSessionPath` — Session path of the parent session the child is attached to.
- `InitialPrompt` — First user-role turn the child sees on boot.
- `Turns` — Optional priming turns appended to the child's context before the start command is published.
- `ContextData` — Optional data-scope entries to seed the child's data context.
- `AutoStart` — When `true` the spawner publishes the start command immediately after building the child.

## Properties

### `AutoStart`

When `true` the spawner publishes the start command immediately after building the child.

### `Config`

Agent config the child runs under.

### `ContextData`

Optional data-scope entries to seed the child's data context.

### `Id`

Child session id (parent agent id + channel name).

### `InitialPrompt`

First user-role turn the child sees on boot.

### `ParentSessionPath`

Session path of the parent session the child is attached to.

### `Turns`

Optional priming turns appended to the child's context before the start command is published.

## Methods

### `PromptedAgentStartInformation`([AgentConfig](AgentConfig.md) Config, [SessionId](Sessions/SessionId.md) Id, [SessionPath](Sessions/SessionPath.md) ParentSessionPath, [ModelTurn](../Provider/ModelTurn.md) InitialPrompt, Nullable<ImmutableArray<[ModelTurn](../Provider/ModelTurn.md)>> Turns, ImmutableDictionary<string, object> ContextData, bool AutoStart)

Bundle of inputs needed to launch a prompted sub-agent (heartbeat, cron,
any tick-driven transient). Captures the config, the parent session path,
the initial user-role turn, optional priming turns, and optional data-scope
entries to seed the child's context.

#### Parameters

- `Config` — Agent config the child runs under.
- `Id` — Child session id (parent agent id + channel name).
- `ParentSessionPath` — Session path of the parent session the child is attached to.
- `InitialPrompt` — First user-role turn the child sees on boot.
- `Turns` — Optional priming turns appended to the child's context before the start command is published.
- `ContextData` — Optional data-scope entries to seed the child's data context.
- `AutoStart` — When `true` the spawner publishes the start command immediately after building the child.

### `CreateDefaultSubAgentConfig`(string name, [AgentConfig](AgentConfig.md) config)

Builds the default sub-agent config for a named transient (e.g. `"heartbeat"` → `HEARTBEAT.md`
for both the system prompt and prompt-context templates).

#### Parameters

- `name` — Sub-agent channel name; uppercased and used as the template stem.
- `config` — Parent agent's config; the returned config is a `with` overlay.

#### Returns

The overlaid config the child should run under.

### `WithMissingPropertiesGenerated`

Returns a copy with any `null`[PromptedAgentStartInformation](PromptedAgentStartInformation.md).`Turns` or
[PromptedAgentStartInformation](PromptedAgentStartInformation.md).`ContextData` replaced with empty collections, so consumers
can iterate without null checks.

