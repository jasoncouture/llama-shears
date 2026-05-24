# LlamaShears.Core.Abstractions.Provider.FilterOptions

Assembly: `LlamaShears.Core.Abstractions`

Policy + name-list pair used by [AgentBehaviorOptions](AgentBehaviorOptions.md) to control
which skills, MCP sources, or MCP tools the agent is allowed to use.
The interpretation of [FilterOptions](FilterOptions.md).`Names` depends on [FilterOptions](FilterOptions.md).`Default`:
[FilterPolicy](FilterPolicy.md).`Allow` treats [FilterOptions](FilterOptions.md).`Names` as an allowlist,
[FilterPolicy](FilterPolicy.md).`Deny` treats it as a denylist,
[FilterPolicy](FilterPolicy.md).`Default` ignores it and lets everything through,
and [FilterPolicy](FilterPolicy.md).`Disable` blocks the entire subsystem.

## Parameters

- `Names` — Names the policy operates on. Treated as an allowlist or denylist depending on `Default`.
- `Default` — Policy applied to `Names` and everything outside it.

## Properties

### `Default`

Policy applied to `Names` and everything outside it.

### `Names`

Names the policy operates on. Treated as an allowlist or denylist depending on `Default`.

## Methods

### `FilterOptions`(HashSet<string> Names, [FilterPolicy](FilterPolicy.md) Default)

Policy + name-list pair used by [AgentBehaviorOptions](AgentBehaviorOptions.md) to control
which skills, MCP sources, or MCP tools the agent is allowed to use.
The interpretation of [FilterOptions](FilterOptions.md).`Names` depends on [FilterOptions](FilterOptions.md).`Default`:
[FilterPolicy](FilterPolicy.md).`Allow` treats [FilterOptions](FilterOptions.md).`Names` as an allowlist,
[FilterPolicy](FilterPolicy.md).`Deny` treats it as a denylist,
[FilterPolicy](FilterPolicy.md).`Default` ignores it and lets everything through,
and [FilterPolicy](FilterPolicy.md).`Disable` blocks the entire subsystem.

#### Parameters

- `Names` — Names the policy operates on. Treated as an allowlist or denylist depending on `Default`.
- `Default` — Policy applied to `Names` and everything outside it.

