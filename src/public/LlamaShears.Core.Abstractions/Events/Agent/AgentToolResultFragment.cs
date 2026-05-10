namespace LlamaShears.Core.Abstractions.Events.Agent;

/// <summary>
/// Event-bus payload describing the outcome of a single tool call.
/// Pairs with <see cref="AgentToolCallFragment"/> via
/// <paramref name="CallId"/> when the provider supplies one.
/// </summary>
/// <param name="Source">Logical owner of the tool that ran.</param>
/// <param name="Name">Tool name within <paramref name="Source"/>.</param>
/// <param name="Result">String body the tool produced.</param>
/// <param name="IsError">Whether the tool reported a failure.</param>
/// <param name="CallId">Provider-supplied correlation id matching the originating call; <see langword="null"/> when the provider does not surface one.</param>
public sealed record AgentToolResultFragment(
    string Source,
    string Name,
    string Result,
    bool IsError = false,
    string? CallId = null);
