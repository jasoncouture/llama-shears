# LlamaShears.Core.Abstractions.Provider.FilterPolicy

Assembly: `LlamaShears.Core.Abstractions`

Selects how a [FilterOptions](FilterOptions.md) instance interprets its name list.

## Fields

### `Allow`

The name list is an allowlist — only names it contains are allowed.

### `Default`

No filtering — every name is allowed and the name list is ignored.

### `Deny`

The name list is a denylist — every name except those it contains is allowed.

### `Disable`

The entire subsystem gated by this filter is turned off; nothing is allowed regardless of the name list.

