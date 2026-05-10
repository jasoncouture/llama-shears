# LlamaShears.Core.Abstractions.Events.Agent.AgentToolResultFragment

Assembly: `LlamaShears.Core.Abstractions`

Event-bus payload describing the outcome of a single tool call.
Pairs with [AgentToolCallFragment](AgentToolCallFragment.md) via
`CallId` when the provider supplies one.

## Parameters

- `Source` — Logical owner of the tool that ran.
- `Name` — Tool name within `Source`.
- `Result` — String body the tool produced.
- `IsError` — Whether the tool reported a failure.
- `CallId` — Provider-supplied correlation id matching the originating call; `null` when the provider does not surface one.

## Properties

### `CallId`

Provider-supplied correlation id matching the originating call; `null` when the provider does not surface one.

### `IsError`

Whether the tool reported a failure.

### `Name`

Tool name within `Source`.

### `Result`

String body the tool produced.

### `Source`

Logical owner of the tool that ran.

## Methods

### `AgentToolResultFragment`(string Source, string Name, string Result, bool IsError, string CallId)

Event-bus payload describing the outcome of a single tool call.
Pairs with [AgentToolCallFragment](AgentToolCallFragment.md) via
`CallId` when the provider supplies one.

#### Parameters

- `Source` — Logical owner of the tool that ran.
- `Name` — Tool name within `Source`.
- `Result` — String body the tool produced.
- `IsError` — Whether the tool reported a failure.
- `CallId` — Provider-supplied correlation id matching the originating call; `null` when the provider does not surface one.

