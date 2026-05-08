using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace LlamaShears.Core.Abstractions.Agent.Sessions;

/// <summary>
/// Identifier for a session: <c>agentId:defaultChannel</c> in canonical
/// string form. Mirrors <c>EventType</c>'s shape — first segment is the
/// agent id, second segment is the default channel for the session, and
/// any further <c>:</c>-separated suffixes are absorbed into the channel
/// part as opaque content (so channel ids that themselves carry colons,
/// e.g. <c>telegram:123456</c>, round-trip cleanly).
/// </summary>
/// <param name="AgentId">Agent the session belongs to.</param>
/// <param name="DefaultChannel">Channel the session is bound to. May contain <c>:</c> — everything after the first <c>:</c> in the canonical form is treated as part of this value.</param>
public sealed partial record SessionId(string AgentId, string DefaultChannel)
{
    [GeneratedRegex("^[a-z0-9_-]+:[a-z0-9_.:-]+$")]
    private static partial Regex GetValidationRegex();

    /// <summary>Implicit conversion to the canonical string form (or <see langword="null"/> for a null receiver).</summary>
    [return: NotNullIfNotNull(nameof(value))]
    public static implicit operator string?(SessionId? value) => value?.ToString();

    /// <summary>
    /// Parses <paramref name="value"/> in the canonical string form.
    /// Throws <see cref="ArgumentException"/> when the input is not a
    /// valid session id — use <see cref="TryParse"/> for non-throwing
    /// parsing.
    /// </summary>
    public static explicit operator SessionId(string value)
    {
        if (!TryParse(value, out var typed))
        {
            throw new ArgumentException($"Invalid session id: {value}", nameof(value));
        }
        return typed;
    }

    /// <summary>
    /// Attempts to parse <paramref name="sessionId"/>. Returns
    /// <see langword="true"/> and assigns <paramref name="typed"/> on
    /// success; otherwise <see langword="false"/> with
    /// <paramref name="typed"/> set to <see langword="null"/>.
    /// </summary>
    public static bool TryParse(string sessionId, [NotNullWhen(true)] out SessionId? typed)
    {
        typed = null;
        if (string.IsNullOrWhiteSpace(sessionId)) return false;
        if (sessionId.Length > 512) return false;
        if (!GetValidationRegex().IsMatch(sessionId)) return false;
        var firstColon = sessionId.IndexOf(':');
        if (firstColon <= 0 || firstColon >= sessionId.Length - 1) return false;
        typed = new SessionId(
            AgentId: sessionId[..firstColon],
            DefaultChannel: sessionId[(firstColon + 1)..]);
        return true;
    }

    /// <inheritdoc/>
    public override string ToString() => $"{AgentId}:{DefaultChannel}";
}
