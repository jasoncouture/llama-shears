# LlamaShears.Core.Abstractions.Agent.Sessions.IAgentData

Assembly: `LlamaShears.Core.Abstractions`

Marker for any value that contributes one or more entries to an agent's per-turn data scope.
Consumers (e.g. `IAgentFactory`) call [IAgentData](IAgentData.md).`GetData` and overlay the entries onto the
scope's keyed dictionary.

## Methods

### `GetData`

Returns the key/value pairs this instance contributes to the agent data scope.

