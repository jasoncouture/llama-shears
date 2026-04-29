namespace LlamaShears.Agent.Abstractions.Events;

/// <summary>
/// A message authored by a user and submitted to a specific agent. Fired
/// on the in-process bus when a UI (or any other producer) wants the
/// agent identified by <paramref name="AgentId"/> to receive the message.
/// </summary>
/// <param name="AgentId">Identifier of the target agent.</param>
/// <param name="Content">The user's message body, verbatim.</param>
/// <param name="At">Wall-clock time the message was submitted.</param>
public sealed record UserMessageSubmitted(string AgentId, string Content, DateTimeOffset At);
