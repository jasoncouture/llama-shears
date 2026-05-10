# LlamaShears.Core.Abstractions.Agent.AgentConfig

Assembly: `LlamaShears.Core.Abstractions`

Immutable on-disk configuration snapshot for one agent. Loaded from
`<Data>/agents/<id>.json` by [IAgentConfigProvider](IAgentConfigProvider.md)
and held for the duration of an in-flight interaction so a single
turn sees one consistent configuration end-to-end.

## Parameters

- `Model` — Language model selection and per-call options.
- `Id` — Stable agent identifier; populated from the file name and not serialized back into the JSON body.
- `WorkspacePath` — Absolute or workspace-relative path to the agent's workspace overlay; `null` falls back to the framework default.
- `SystemPrompt` — File name (including extension) of the system-prompt template to render, e.g. `DEFAULT.md`; `null` uses `DEFAULT.md`.
- `PromptContext` — Name of the per-turn prompt-context template; `null` uses `PROMPT`.
- `Embedding` — Embedding model selection used for memory search; `null` disables memory features.
- `ModelContextProtocolServers` — Set of MCP server names this agent is allowed to call; `null` grants no MCP access.

## Fields

### `DataKey`

Key used to stash the active [AgentConfig](AgentConfig.md) in the per-turn data context scope.

## Properties

### `Embedding`

Embedding model selection used for memory search; `null` disables memory features.

### `HeartbeatPeriod`

How often the host injects a heartbeat turn into an idle agent. Defaults to 30 minutes.

### `Id`

Stable agent identifier; populated from the file name and not serialized back into the JSON body.

### `Memory`

Memory-subsystem options (e.g. eager prefetch).

### `Model`

Language model selection and per-call options.

### `ModelContextProtocolServers`

Set of MCP server names this agent is allowed to call; `null` grants no MCP access.

### `PromptContext`

Name of the per-turn prompt-context template; `null` uses `PROMPT`.

### `SystemPrompt`

File name (including extension) of the system-prompt template to render, e.g. `DEFAULT.md`; `null` uses `DEFAULT.md`.

### `Tools`

Tool-loop budget and related guardrails for this agent.

### `WorkspacePath`

Absolute or workspace-relative path to the agent's workspace overlay; `null` falls back to the framework default.

## Methods

### `AgentConfig`([ModelConfiguration](../Provider/ModelConfiguration.md) Model, string Id, string WorkspacePath, string SystemPrompt, string PromptContext, [ModelConfiguration](../Provider/ModelConfiguration.md) Embedding, ImmutableHashSet<string> ModelContextProtocolServers)

Immutable on-disk configuration snapshot for one agent. Loaded from
`<Data>/agents/<id>.json` by [IAgentConfigProvider](IAgentConfigProvider.md)
and held for the duration of an in-flight interaction so a single
turn sees one consistent configuration end-to-end.

#### Parameters

- `Model` — Language model selection and per-call options.
- `Id` — Stable agent identifier; populated from the file name and not serialized back into the JSON body.
- `WorkspacePath` — Absolute or workspace-relative path to the agent's workspace overlay; `null` falls back to the framework default.
- `SystemPrompt` — File name (including extension) of the system-prompt template to render, e.g. `DEFAULT.md`; `null` uses `DEFAULT.md`.
- `PromptContext` — Name of the per-turn prompt-context template; `null` uses `PROMPT`.
- `Embedding` — Embedding model selection used for memory search; `null` disables memory features.
- `ModelContextProtocolServers` — Set of MCP server names this agent is allowed to call; `null` grants no MCP access.

