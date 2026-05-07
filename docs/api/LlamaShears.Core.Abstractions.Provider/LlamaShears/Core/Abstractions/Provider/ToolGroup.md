# LlamaShears.Core.Abstractions.Provider.ToolGroup

Assembly: `LlamaShears.Core.Abstractions.Provider`

A bundle of [ToolDescriptor](ToolDescriptor.md)s sharing a single
[ToolGroup](ToolGroup.md).`Source` (e.g. an MCP server slug). Used as the
prompt-time grouping; the framework dispatches tool calls by
pairing [ToolGroup](ToolGroup.md).`Source` with the model-supplied tool name.

## Parameters

- `Source` — Logical owner / dispatch key for the tools in this group.
- `Tools` — Tools owned by this source.

## Properties

### `Source`

Logical owner / dispatch key for the tools in this group.

### `Tools`

Tools owned by this source.

## Methods

### `ToolGroup`(string Source, ImmutableArray<[ToolDescriptor](ToolDescriptor.md)> Tools)

A bundle of [ToolDescriptor](ToolDescriptor.md)s sharing a single
[ToolGroup](ToolGroup.md).`Source` (e.g. an MCP server slug). Used as the
prompt-time grouping; the framework dispatches tool calls by
pairing [ToolGroup](ToolGroup.md).`Source` with the model-supplied tool name.

#### Parameters

- `Source` — Logical owner / dispatch key for the tools in this group.
- `Tools` — Tools owned by this source.

