namespace LlamaShears.Core.Abstractions.Events.Agent;

/// <summary>
/// Marker interface implemented by every event payload an agent emits onto
/// the bus — fragments, lifecycle markers, compaction markers, and so on.
/// Subscribers use it as a single subscription point for "anything an agent
/// said" without enumerating concrete fragment types.
/// </summary>
public interface IAgentMessage;
