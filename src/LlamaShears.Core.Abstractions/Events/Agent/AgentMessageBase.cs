namespace LlamaShears.Core.Abstractions.Events.Agent;

/// <summary>
/// Common shape for agent-emitted message and thought fragments
/// flowing through the event bus. Concrete subtypes
/// (<see cref="AgentMessageFragment"/>, <see cref="AgentThoughtFragment"/>)
/// add stream-specific metadata.
/// </summary>
/// <param name="Content">Body text of this fragment.</param>
/// <param name="ChannelId">Optional channel correlation id; <see langword="null"/> when not channel-bound.</param>
public abstract record AgentMessageBase(string Content, string? ChannelId = null);
