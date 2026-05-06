# LlamaShears.Core.Abstractions.Context.ToolParameter

Assembly: `LlamaShears.Core.Abstractions.Tools`

One parameter in a [ToolDescriptor](ToolDescriptor.md)'s schema.

## Parameters

- `Name` — Parameter name.
- `Description` — Human-readable description shown to the model.
- `Type` — JSON-schema-flavored type tag (e.g. `"string"`, `"integer"`).
- `Required` — Whether the parameter must be supplied for the call to be valid.

## Properties

### `Description`

Human-readable description shown to the model.

### `Name`

Parameter name.

### `Required`

Whether the parameter must be supplied for the call to be valid.

### `Type`

JSON-schema-flavored type tag (e.g. `"string"`, `"integer"`).

## Methods

### `ToolParameter`(string Name, string Description, string Type, bool Required)

One parameter in a [ToolDescriptor](ToolDescriptor.md)'s schema.

#### Parameters

- `Name` — Parameter name.
- `Description` — Human-readable description shown to the model.
- `Type` — JSON-schema-flavored type tag (e.g. `"string"`, `"integer"`).
- `Required` — Whether the parameter must be supplied for the call to be valid.

