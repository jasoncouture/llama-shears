# LlamaShears.Core.Abstractions.Agent.AgentConfiguration

Assembly: `LlamaShears.Core.Abstractions`

Construction-time inputs for [IAgentFactory](IAgentFactory.md).`CreateAgent`.
Carries the agent identifier and a free-form parameter bag so plugin
factories can receive options without growing the framework contract.

## Parameters

- `AgentId` — Identifier of the agent to construct.
- `Parameters` — Optional free-form construction parameters; `null` = none.

## Properties

### `AgentId`

Identifier of the agent to construct.

### `Parameters`

Optional free-form construction parameters; `null` = none.

## Methods

### `AgentConfiguration`(string AgentId, IReadOnlyDictionary<string, object> Parameters)

Construction-time inputs for [IAgentFactory](IAgentFactory.md).`CreateAgent`.
Carries the agent identifier and a free-form parameter bag so plugin
factories can receive options without growing the framework contract.

#### Parameters

- `AgentId` — Identifier of the agent to construct.
- `Parameters` — Optional free-form construction parameters; `null` = none.

