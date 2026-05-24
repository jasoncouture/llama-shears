# LlamaShears.Core.Abstractions.Provider.ToolDescriptorAnnotations

Assembly: `LlamaShears.Core.Abstractions`

Behavior hints attached to a [ToolDescriptor](ToolDescriptor.md). Mirrors the
optional MCP `ToolAnnotations` block (`destructiveHint` /
`idempotentHint` / `openWorldHint` / `readOnlyHint`) so
host code, UI surfaces, and confirmation gates can reason about a tool's
side-effect profile without re-deriving it from the schema. Values default
to the MCP-spec defaults so an absent annotation block degrades to the
conservative interpretation.

## Parameters

- `Title` — Human-friendly title for the tool. Sources should populate this from the MCP `title` annotation when present, otherwise fall back to the tool's own `title` or its `name`.
- `Destructive` — Indicates the tool might perform destructive updates to its environment. Defaults to `true` (per MCP spec); only relevant when `ReadOnly` is `false`.
- `Idempotent` — Indicates that repeated invocations with identical arguments have no additional effect. Defaults to `false`; only relevant when `ReadOnly` is `false`.
- `OpenWorld` — Indicates the tool can interact with an unpredictable / dynamic set of external entities (e.g. a web search or shell). Defaults to `true`; set to `false` for tools whose domain is closed and well-defined (e.g. workspace files, memory store).
- `ReadOnly` — Indicates the tool performs only reads and never modifies its environment. Defaults to `false`.

## Properties

### `Destructive`

Indicates the tool might perform destructive updates to its environment. Defaults to `true` (per MCP spec); only relevant when `ReadOnly` is `false`.

### `Idempotent`

Indicates that repeated invocations with identical arguments have no additional effect. Defaults to `false`; only relevant when `ReadOnly` is `false`.

### `OpenWorld`

Indicates the tool can interact with an unpredictable / dynamic set of external entities (e.g. a web search or shell). Defaults to `true`; set to `false` for tools whose domain is closed and well-defined (e.g. workspace files, memory store).

### `ReadOnly`

Indicates the tool performs only reads and never modifies its environment. Defaults to `false`.

### `Title`

Human-friendly title for the tool. Sources should populate this from the MCP `title` annotation when present, otherwise fall back to the tool's own `title` or its `name`.

## Methods

### `ToolDescriptorAnnotations`(string Title, bool Destructive, bool Idempotent, bool OpenWorld, bool ReadOnly)

Behavior hints attached to a [ToolDescriptor](ToolDescriptor.md). Mirrors the
optional MCP `ToolAnnotations` block (`destructiveHint` /
`idempotentHint` / `openWorldHint` / `readOnlyHint`) so
host code, UI surfaces, and confirmation gates can reason about a tool's
side-effect profile without re-deriving it from the schema. Values default
to the MCP-spec defaults so an absent annotation block degrades to the
conservative interpretation.

#### Parameters

- `Title` — Human-friendly title for the tool. Sources should populate this from the MCP `title` annotation when present, otherwise fall back to the tool's own `title` or its `name`.
- `Destructive` — Indicates the tool might perform destructive updates to its environment. Defaults to `true` (per MCP spec); only relevant when `ReadOnly` is `false`.
- `Idempotent` — Indicates that repeated invocations with identical arguments have no additional effect. Defaults to `false`; only relevant when `ReadOnly` is `false`.
- `OpenWorld` — Indicates the tool can interact with an unpredictable / dynamic set of external entities (e.g. a web search or shell). Defaults to `true`; set to `false` for tools whose domain is closed and well-defined (e.g. workspace files, memory store).
- `ReadOnly` — Indicates the tool performs only reads and never modifies its environment. Defaults to `false`.

