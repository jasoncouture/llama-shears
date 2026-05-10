namespace LlamaShears.Core.Abstractions.Events.Agent;

/// <summary>
/// One streaming chunk of agent-visible text emitted as the model
/// produces its response. Subscribers concatenate fragments in
/// arrival order to reconstruct the final assistant message.
/// </summary>
/// <param name="Content">Text of this fragment.</param>
/// <param name="ChannelId">Optional channel correlation id; <see langword="null"/> when not channel-bound.</param>
/// <param name="Final">Whether this is the last fragment for the current message stream.</param>
public sealed record AgentMessageFragment(string Content, string? ChannelId = null, bool Final = false)
    : AgentMessageBase(Content, ChannelId);
