# LlamaShears.Core.Abstractions.Events.Agent.AgentToolCallFragment

Assembly: `LlamaShears.Core.Abstractions.Events`

Event-bus payload describing a single tool call the agent is about
to dispatch. Mirrors the provider-layer ToolCall record; kept
distinct so consumers of the event bus don't have to depend on the
provider layer.

## Parameters

- `Source` — Logical owner of the tool (e.g. an MCP server slug).
- `Name` — Tool name within `Source`.
- `ArgumentsJson` — Tool arguments serialized as JSON exactly as the model produced them.
- `CallId` — Provider-supplied correlation id; `null` when the provider does not surface one.

## Properties

### `ArgumentsJson`

Tool arguments serialized as JSON exactly as the model produced them.

### `CallId`

Provider-supplied correlation id; `null` when the provider does not surface one.

### `Name`

Tool name within `Source`.

### `Source`

Logical owner of the tool (e.g. an MCP server slug).

## Methods

### `AgentToolCallFragment`(string Source, string Name, string ArgumentsJson, string CallId)

Event-bus payload describing a single tool call the agent is about
to dispatch. Mirrors the provider-layer ToolCall record; kept
distinct so consumers of the event bus don't have to depend on the
provider layer.

#### Parameters

- `Source` — Logical owner of the tool (e.g. an MCP server slug).
- `Name` — Tool name within `Source`.
- `ArgumentsJson` — Tool arguments serialized as JSON exactly as the model produced them.
- `CallId` — Provider-supplied correlation id; `null` when the provider does not surface one.

