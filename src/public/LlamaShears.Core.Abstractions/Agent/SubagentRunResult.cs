namespace LlamaShears.Core.Abstractions.Agent;

/// <summary>
/// Outcome of <see cref="ISubagentRunner.RunAsync"/>.
/// <see cref="Ok"/> means the child finished while the caller
/// waited. <see cref="Error"/> is refuse / spawn / timeout
/// transport — not the child's HTTP-style status.
/// </summary>
/// <param name="Ok"><see langword="true"/> when the awaited child reached idle.</param>
/// <param name="SessionId">Canonical child session id, when a session was created.</param>
/// <param name="Awaited"><see langword="true"/> when the caller asked to wait.</param>
/// <param name="Output">Last assistant text captured from the child, if any.</param>
/// <param name="TimedOut"><see langword="true"/> when the await ceiling elapsed.</param>
/// <param name="Error">Refuse, spawn, or timeout explanation; <see langword="null"/> on success.</param>
/// <param name="Started"><see langword="true"/> when the child session was spawned.</param>
public sealed record SubagentRunResult(
    bool Ok,
    string? SessionId,
    bool Awaited,
    string? Output,
    bool TimedOut,
    string? Error,
    bool Started = false);
