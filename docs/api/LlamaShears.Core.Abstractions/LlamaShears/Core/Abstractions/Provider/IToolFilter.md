# LlamaShears.Core.Abstractions.Provider.IToolFilter

Assembly: `LlamaShears.Core.Abstractions`

Gatekeeps which MCP tools the agent sees during tool discovery. Applied
inside the discovery pipeline so blocked tools never reach the language
model's tool list — the model cannot select what it cannot see.
Implementations can short-circuit at three levels: all tools, all tools
from a given source, or a specific tool by name.

## Methods

### `AreToolsAllowed`

Indicates whether any tools should be exposed in the current context.
Returning `false` short-circuits discovery and yields an empty tool list.

#### Returns

`true` when tools should be exposed; otherwise `false`.

### `IsSourceAllowed`(string source)

Indicates whether any tool from the given MCP source should be exposed.
Sources are the MCP server names the agent is configured to consume
(for example `llamashears` for the internal server, or an external server name).

#### Parameters

- `source` — MCP source / server name.

#### Returns

`true` when tools from the source should be exposed; otherwise `false`.

### `IsToolAllowed`(string source, string toolName)

Indicates whether a specific tool from a given source should be exposed.
Invoked once per discovered tool after [IToolFilter](IToolFilter.md).`AreToolsAllowed` and
[IToolFilter](IToolFilter.md).`IsSourceAllowed` have both returned `true`.

#### Parameters

- `source` — MCP source / server name.
- `toolName` — The tool's declared name as returned by `list_tools`.

#### Returns

`true` when the named tool should be exposed; otherwise `false`.

