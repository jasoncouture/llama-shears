# LlamaShears.Core.Abstractions.Events.IEventPublisher

Assembly: `LlamaShears.Core.Abstractions`

Publishes events to the in-process bus. Implementations are expected to
fan out each call to both fire-and-forget and awaited delivery so
subscribers can opt into either mode.

