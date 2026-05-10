# LlamaShears.Core.Abstractions.Provider.ToolDescriptor

Assembly: `LlamaShears.Core.Abstractions`

Describes one callable tool: its name, what it does (for the
model), and its parameter schema.

## Parameters

- `Name` — Tool name; combined with the owning [ToolGroup](ToolGroup.md).`Source` to dispatch a call.
- `Description` — Human-readable description shown to the model so it knows when to invoke the tool.
- `Parameters` — Parameter schema in declaration order.

## Properties

### `Description`

Human-readable description shown to the model so it knows when to invoke the tool.

### `Name`

Tool name; combined with the owning [ToolGroup](ToolGroup.md).`Source` to dispatch a call.

### `Parameters`

Parameter schema in declaration order.

## Methods

### `ToolDescriptor`(string Name, string Description, ImmutableArray<[ToolParameter](ToolParameter.md)> Parameters)

Describes one callable tool: its name, what it does (for the
model), and its parameter schema.

#### Parameters

- `Name` — Tool name; combined with the owning [ToolGroup](ToolGroup.md).`Source` to dispatch a call.
- `Description` — Human-readable description shown to the model so it knows when to invoke the tool.
- `Parameters` — Parameter schema in declaration order.

