# LlamaShears.Core.Abstractions.Events.Agent.AgentDeath

Assembly: `LlamaShears.Core.Abstractions`

Singleton payload for [Lifecycle](../Event/WellKnown/Lifecycle.md).`Death`. The agent id lives
on the envelope's `EventType.Id`; the payload carries no further data.

## Properties

### `Instance`

Singleton instance — subscribers never need a distinct instance per event.

