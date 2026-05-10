namespace LlamaShears.Core.Abstractions.Events.Agent;

/// <summary>
/// One streaming chunk of hidden chain-of-thought emitted by a
/// thinking-capable model. Surfaced for visibility but never replayed
/// back into a later prompt.
/// </summary>
/// <param name="Content">Reasoning text in this fragment.</param>
/// <param name="ChannelId">Optional channel correlation id; <see langword="null"/> when not channel-bound.</param>
/// <param name="Final">Whether this is the last fragment for the current thought stream.</param>
public sealed record AgentThoughtFragment(string Content, string? ChannelId = null, bool Final = false)
    : AgentMessageBase(Content, ChannelId);
