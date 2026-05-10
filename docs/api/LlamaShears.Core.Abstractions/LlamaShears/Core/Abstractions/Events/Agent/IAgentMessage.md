# LlamaShears.Core.Abstractions.Events.Agent.IAgentMessage

Assembly: `LlamaShears.Core.Abstractions`

Marker interface implemented by every event payload an agent emits onto
the bus — fragments, lifecycle markers, compaction markers, and so on.
Subscribers use it as a single subscription point for "anything an agent
said" without enumerating concrete fragment types.

