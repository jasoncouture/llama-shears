# LlamaShears.Core.Abstractions.Provider.ToolDescriptor

Assembly: `LlamaShears.Core.Abstractions`

Describes one callable tool: its name, what it does (for the
model), and its parameter schema.

## Parameters

- `Name` — Tool name; combined with the owning [ToolGroup](ToolGroup.md).`Source` to dispatch a call.
- `Description` — Human-readable description shown to the model so it knows when to invoke the tool.
- `Parameters` — Parsed parameter list in declaration order; lossy projection of `Schema` kept for caller-side introspection (required-ness, top-level type names).
- `Schema` — Raw JSON Schema for the tool's parameters as the source advertised it. Providers should forward this verbatim instead of rebuilding from `Parameters`, since rebuilding drops fields strict validators (Gemini, structured-output) require. JsonValueKind.`Undefined` means the source advertised no schema.

## Properties

### `Description`

Human-readable description shown to the model so it knows when to invoke the tool.

### `Name`

Tool name; combined with the owning [ToolGroup](ToolGroup.md).`Source` to dispatch a call.

### `Parameters`

Parsed parameter list in declaration order; lossy projection of `Schema` kept for caller-side introspection (required-ness, top-level type names).

### `Schema`

Raw JSON Schema for the tool's parameters as the source advertised it. Providers should forward this verbatim instead of rebuilding from `Parameters`, since rebuilding drops fields strict validators (Gemini, structured-output) require. JsonValueKind.`Undefined` means the source advertised no schema.

## Methods

### `ToolDescriptor`(string Name, string Description, ImmutableArray<[ToolParameter](ToolParameter.md)> Parameters, JsonElement Schema)

Describes one callable tool: its name, what it does (for the
model), and its parameter schema.

#### Parameters

- `Name` — Tool name; combined with the owning [ToolGroup](ToolGroup.md).`Source` to dispatch a call.
- `Description` — Human-readable description shown to the model so it knows when to invoke the tool.
- `Parameters` — Parsed parameter list in declaration order; lossy projection of `Schema` kept for caller-side introspection (required-ness, top-level type names).
- `Schema` — Raw JSON Schema for the tool's parameters as the source advertised it. Providers should forward this verbatim instead of rebuilding from `Parameters`, since rebuilding drops fields strict validators (Gemini, structured-output) require. JsonValueKind.`Undefined` means the source advertised no schema.

