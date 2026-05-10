# LlamaShears.Core.Abstractions.Agent.IAgentTokenStore

Assembly: `LlamaShears.Core.Abstractions.Agent`

In-process store that issues opaque single-use bearer tokens bound to an
AgentInfo. Tokens are valid until consumed (via
[IAgentTokenStore](IAgentTokenStore.md).`TryGetAgentInformation`) or until they expire — whichever
comes first.

## Methods

### `Issue`(AgentInfo agent)

Issue a fresh token bound to `agent`. The token is a
base64-encoded opaque string. Callers must treat it as a credential.

### `TryGetAgentInformation`(string token, AgentInfo& agent)

Atomically consume `token`: if it is a known and
unexpired token, return its bound AgentInfo and remove
the entry from the store. Subsequent calls with the same token return
false.

