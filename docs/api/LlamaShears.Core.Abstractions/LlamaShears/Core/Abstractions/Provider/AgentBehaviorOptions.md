# LlamaShears.Core.Abstractions.Provider.AgentBehaviorOptions

Assembly: `LlamaShears.Core.Abstractions`

Per-agent security/behavior gate applied during skill and MCP tool discovery.
Each [FilterOptions](FilterOptions.md) sub-block independently gates a subsystem
(skills, tool invocation, tool sources) via a policy + name list.

## Parameters

- `Skills` — Filter applied to skill names returned by the skill repository.
- `Tools` — Filter applied to MCP tool names returned by tool discovery.
- `Sources` — Filter applied to MCP source / server names before any of their tools are discovered.

## Properties

### `Skills`

Filter applied to skill names returned by the skill repository.

### `Sources`

Filter applied to MCP source / server names before any of their tools are discovered.

### `Tools`

Filter applied to MCP tool names returned by tool discovery.

## Methods

### `AgentBehaviorOptions`([FilterOptions](FilterOptions.md) Skills, [FilterOptions](FilterOptions.md) Tools, [FilterOptions](FilterOptions.md) Sources)

Per-agent security/behavior gate applied during skill and MCP tool discovery.
Each [FilterOptions](FilterOptions.md) sub-block independently gates a subsystem
(skills, tool invocation, tool sources) via a policy + name list.

#### Parameters

- `Skills` — Filter applied to skill names returned by the skill repository.
- `Tools` — Filter applied to MCP tool names returned by tool discovery.
- `Sources` — Filter applied to MCP source / server names before any of their tools are discovered.

