# LlamaShears.Core.Abstractions.Events.Agent.AgentLifecycleMarker

Assembly: `LlamaShears.Core.Abstractions`

Empty payload for the agent lifecycle events
([Agent](../Event/WellKnown/Agent.md).`Loaded`,
[Agent](../Event/WellKnown/Agent.md).`Unloaded`,
[Agent](../Event/WellKnown/Agent.md).`LoadError`).
Carries no data — its presence on the bus, with the agent id on
[EventType](../EventType.md).`Id`, is the signal.

## Properties

### `Instance`

Singleton marker; subscribers never need a distinct instance per event.

