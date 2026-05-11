# LlamaShears.Core.Abstractions.Agent.IAgentConfigProvider

Assembly: `LlamaShears.Core.Abstractions`

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

### `ReadFileAsync`(string agentId, CancellationToken cancellationToken)

Returns the raw JSON text of the agent's config file plus a hash
of the bytes, or `null` when no file exists.
The hash is the change token [IAgentConfigProvider](IAgentConfigProvider.md).`SaveAsync` validates
against; pair this call with a later save to detect concurrent
edits to the same file.

### `SaveAsync`(string agentId, string expectedHash, string content, CancellationToken cancellationToken)

Writes `content` to the agent's config file if
the current on-disk hash equals `expectedHash`
(case-insensitive) and the content deserializes to an
[AgentConfig](AgentConfig.md). Returns the outcome:
[Ok](SaveAgentConfigResult/Ok.md) on success,
[Conflict](SaveAgentConfigResult/Conflict.md) when the hash
doesn't match, or [InvalidJson](SaveAgentConfigResult/InvalidJson.md)
when the content fails validation.

