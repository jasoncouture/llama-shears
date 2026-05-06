# LlamaShears.Core.Abstractions.Context

## Types

- [ToolDescriptor](ToolDescriptor.md) — Describes one callable tool: its name, what it does (for the model), and its parameter schema.
- [ToolGroup](ToolGroup.md) — A bundle of [ToolDescriptor](ToolDescriptor.md)s sharing a single [ToolGroup](ToolGroup.md).`Source` (e.g. an MCP server slug). Used as the prompt-time grouping; the framework dispatches tool calls by pairing [ToolGroup](ToolGroup.md).`Source` with the model-supplied tool name.
- [ToolParameter](ToolParameter.md) — One parameter in a [ToolDescriptor](ToolDescriptor.md)'s schema.

