# LlamaShears.Core.Abstractions.Agent.IAgentManager

Assembly: `LlamaShears.Core.Abstractions.Agent`

Read-only view onto the set of agents currently loaded by the
host. Consumers can list agent ids and check whether a given id
resolves to a loaded agent. The lifecycle (loading/unloading,
reconciliation) is owned by the implementation and not part of
this surface.

## Properties

### `AgentIds`

Snapshot of every agent id currently loaded, in stable
ordinal-ignore-case order.

## Methods

### `Contains`(string agentId)

Returns `true` if an agent with the given id
is currently loaded.

### `Get`(string agentId)

Returns the loaded agent with the given id, or `null`
if none is loaded under that id.

