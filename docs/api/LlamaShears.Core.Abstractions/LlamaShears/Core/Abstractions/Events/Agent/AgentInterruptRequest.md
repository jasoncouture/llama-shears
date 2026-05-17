# LlamaShears.Core.Abstractions.Events.Agent.AgentInterruptRequest

Assembly: `LlamaShears.Core.Abstractions`

Payload for [Command](../Event/WellKnown/Command.md).`InterruptAgent`.
Carries no data — its presence on the bus, with the agent id on
[EventType](../EventType.md).`Id`, is the signal.

## Properties

### `Instance`

Singleton marker; subscribers never need a distinct instance per event.

