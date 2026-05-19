# LlamaShears.Core.Abstractions.Agent.Sessions.SessionDataExtensions

Assembly: `LlamaShears.Core.Abstractions`

Extensions that overlay an [IAgentData](IAgentData.md)'s entries onto a target dictionary.

## Methods

### `ApplyTo`([IAgentData](IAgentData.md) data, IDictionary<string, object> state)

Writes every entry from `data`'s [IAgentData](IAgentData.md).`GetData` into
`state`, replacing any existing values under the same keys.

