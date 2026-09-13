# LlamaShears.Core.Abstractions.Agent.SubagentRunRequest

Assembly: `LlamaShears.Core.Abstractions`

Inputs for [ISubagentRunner](ISubagentRunner.md).`RunAsync`.

## Parameters

- `Prompt` — User-role instruction for the child. Required.
- `Model` — Optional `provider/model` overlay for the child. Unparseable
values are refused. Not a free-form URL.
- `Context` — Optional preamble prepended to `Prompt`.
- `MaxTurns` — Optional child [AgentToolConfig](AgentToolConfig.md).`TurnLimit` (1–64).
Omitted keeps the parent's tool budget.
- `AwaitResult` — When `true` (default), wait for the child to
finish and return its last assistant text. When
`false`, return after spawn.
- `TimeoutSeconds` — Await ceiling in seconds (1–600). Default 120. Ignored when
`AwaitResult` is `false`.
- `SystemPrompt` — Optional system-prompt template file name (e.g.
`COMPACTION.md`). Omitted keeps `SUBAGENT.md`. Must not
contain path separators.
- `PromptContext` — Optional prompt-context template file name (e.g.
`COMPACTION.md`). Omitted keeps `SUBAGENT.md`. Must not
contain path separators.

## Fields

### `DefaultTimeoutSeconds`

Default await ceiling when the caller omits a timeout.

### `MaxMaxTurns`

Largest accepted [SubagentRunRequest](SubagentRunRequest.md).`MaxTurns`.

### `MaxTimeoutSeconds`

Largest accepted [SubagentRunRequest](SubagentRunRequest.md).`TimeoutSeconds`.

### `MinMaxTurns`

Smallest accepted [SubagentRunRequest](SubagentRunRequest.md).`MaxTurns`.

### `MinTimeoutSeconds`

Smallest accepted [SubagentRunRequest](SubagentRunRequest.md).`TimeoutSeconds`.

## Properties

### `AwaitResult`

When `true` (default), wait for the child to
finish and return its last assistant text. When
`false`, return after spawn.

### `Context`

Optional preamble prepended to `Prompt`.

### `MaxTurns`

Optional child [AgentToolConfig](AgentToolConfig.md).`TurnLimit` (1–64).
Omitted keeps the parent's tool budget.

### `Model`

Optional `provider/model` overlay for the child. Unparseable
values are refused. Not a free-form URL.

### `Prompt`

User-role instruction for the child. Required.

### `PromptContext`

Optional prompt-context template file name (e.g.
`COMPACTION.md`). Omitted keeps `SUBAGENT.md`. Must not
contain path separators.

### `SystemPrompt`

Optional system-prompt template file name (e.g.
`COMPACTION.md`). Omitted keeps `SUBAGENT.md`. Must not
contain path separators.

### `TimeoutSeconds`

Await ceiling in seconds (1–600). Default 120. Ignored when
`AwaitResult` is `false`.

## Methods

### `SubagentRunRequest`(string Prompt, string Model, string Context, Nullable<int> MaxTurns, bool AwaitResult, int TimeoutSeconds, string SystemPrompt, string PromptContext)

Inputs for [ISubagentRunner](ISubagentRunner.md).`RunAsync`.

#### Parameters

- `Prompt` — User-role instruction for the child. Required.
- `Model` — Optional `provider/model` overlay for the child. Unparseable
values are refused. Not a free-form URL.
- `Context` — Optional preamble prepended to `Prompt`.
- `MaxTurns` — Optional child [AgentToolConfig](AgentToolConfig.md).`TurnLimit` (1–64).
Omitted keeps the parent's tool budget.
- `AwaitResult` — When `true` (default), wait for the child to
finish and return its last assistant text. When
`false`, return after spawn.
- `TimeoutSeconds` — Await ceiling in seconds (1–600). Default 120. Ignored when
`AwaitResult` is `false`.
- `SystemPrompt` — Optional system-prompt template file name (e.g.
`COMPACTION.md`). Omitted keeps `SUBAGENT.md`. Must not
contain path separators.
- `PromptContext` — Optional prompt-context template file name (e.g.
`COMPACTION.md`). Omitted keeps `SUBAGENT.md`. Must not
contain path separators.
- `SystemPrompt` — Optional system-prompt template file name (e.g.
`COMPACTION.md`). Omitted keeps `SUBAGENT.md`. Must not
contain path separators.
- `PromptContext` — Optional prompt-context template file name (e.g.
`COMPACTION.md`). Omitted keeps `SUBAGENT.md`. Must not
contain path separators.

