# LlamaShears.Core.Abstractions.Provider.ToolCall

Assembly: `LlamaShears.Core.Abstractions`

One tool the model is asking the host to invoke. The host pairs
[ToolCall](ToolCall.md).`Source` + [ToolCall](ToolCall.md).`Name` against the registered
tool catalog to find the right handler.

## Parameters

- `Source` — Logical group the tool belongs to (e.g. an MCP server slug or framework prefix).
- `Name` — Tool name within `Source`.
- `ArgumentsJson` — Tool arguments serialized as JSON exactly as the model produced them.
- `CallId` — Provider-supplied correlation id; used to pair a call with its result. `null` when the provider does not surface one.

## Properties

### `ArgumentsJson`

Tool arguments serialized as JSON exactly as the model produced them.

### `CallId`

Provider-supplied correlation id; used to pair a call with its result. `null` when the provider does not surface one.

### `Name`

Tool name within `Source`.

### `Source`

Logical group the tool belongs to (e.g. an MCP server slug or framework prefix).

## Methods

### `ToolCall`(string Source, string Name, string ArgumentsJson, string CallId)

One tool the model is asking the host to invoke. The host pairs
[ToolCall](ToolCall.md).`Source` + [ToolCall](ToolCall.md).`Name` against the registered
tool catalog to find the right handler.

#### Parameters

- `Source` — Logical group the tool belongs to (e.g. an MCP server slug or framework prefix).
- `Name` — Tool name within `Source`.
- `ArgumentsJson` — Tool arguments serialized as JSON exactly as the model produced them.
- `CallId` — Provider-supplied correlation id; used to pair a call with its result. `null` when the provider does not surface one.

