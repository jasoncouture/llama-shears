namespace LlamaShears.Core.Abstractions.Context;

/// <summary>
/// Per-cycle slice of the agent context. Populated while the agent is
/// processing a turn and null between turns. Holds whatever state is
/// only meaningful for the in-flight cycle (the turn start time today;
/// in-flight tool calls, accumulated fragments, etc. as the runtime
/// grows).
/// </summary>
public sealed record TurnContext(DateTimeOffset Started);
