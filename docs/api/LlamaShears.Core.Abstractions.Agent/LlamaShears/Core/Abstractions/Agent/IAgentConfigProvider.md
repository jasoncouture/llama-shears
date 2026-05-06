# LlamaShears.Core.Abstractions.Agent.IAgentConfigProvider

Assembly: `LlamaShears.Core.Abstractions.Agent`

Source of truth for agent configuration. Reads from the configured
agents directory (`<Data>/agents/<id>.json`) and is
the single read API for both "what agents exist" and "what's the
config for this agent". Implementations may cache by file metadata
but must reflect on-disk edits without a host restart.

## Methods

### `GetConfigAsync`(string agentId, CancellationToken cancellationToken)

Returns the parsed [AgentConfig](AgentConfig.md) for
`agentId`, or `null` if no
config file exists for that id or the existing file fails to
parse.

### `ListAgentIds`

Returns the agent ids currently configured on disk, in stable
lexicographic order.

