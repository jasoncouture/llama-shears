namespace LlamaShears.Agent.Abstractions.Events;

/// <summary>
/// A streaming fragment produced by an agent during a single response
/// cycle. Consumers can append <paramref name="Delta"/> to a per-stream
/// buffer keyed by <paramref name="StreamId"/> and finalize the stream
/// when an event with <paramref name="IsFinal"/> set arrives.
/// <para>
/// Fragments are advisory: a complete <see cref="AgentTurnEmitted"/> is
/// always emitted at end of cycle and should be treated as the
/// authoritative content for the turn.
/// </para>
/// </summary>
/// <param name="AgentId">Identifier of the agent emitting the fragment.</param>
/// <param name="Kind">Whether the fragment is visible text or thought.</param>
/// <param name="Delta">
/// The text appended in this fragment. Empty on the final marker; may
/// also be empty on intermediate fragments depending on the provider.
/// </param>
/// <param name="StreamId">
/// Per-cycle, per-kind stream identifier. Distinguishes the text stream
/// from the thought stream within a single response cycle, and avoids
/// confusion if cycles overlap on a consumer.
/// </param>
/// <param name="IsFinal">
/// <see langword="true"/> on the terminal fragment for this stream so
/// consumers can seal their buffer.
/// </param>
public sealed record AgentFragmentEmitted(
    string AgentId,
    AgentFragmentKind Kind,
    string Delta,
    Guid StreamId,
    bool IsFinal);
