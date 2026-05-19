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
/// <param name="Name">Channel the session is bound to. May contain <c>:</c> — everything after the first <c>:</c> in the canonical form is treated as part of this value.</param>
public sealed partial record SessionId(string AgentId, string Name)
{
    /// <summary>Unique identifier for this session instance. Auto-generated as a v7 GUID when not supplied.</summary>
    public Guid Id { get; init; } = Guid.CreateVersion7();

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
        var secondColon = sessionId.IndexOf(':', firstColon + 1);
        if (secondColon <= firstColon + 1 || secondColon >= sessionId.Length - 1) return false;
        if (!Guid.TryParse(sessionId.AsSpan(firstColon + 1, secondColon - firstColon - 1), out var parsedId))
            return false;
        typed = new SessionId(
            AgentId: sessionId[..firstColon],
            Name: sessionId[(secondColon + 1)..])
        {
            Id = parsedId,
        };
        return true;
    }

    /// <inheritdoc/>
    public override string ToString() => $"{AgentId}:{Id}:{Name}";

    /// <summary>Well-known key under which a <see cref="SessionId"/> is stashed on an agent's data scope.</summary>
    public const string DataKey = "session";

    /// <summary>Name used for the default (main) session of an agent.</summary>
    public const string DefaultSessionName = "default";

    /// <summary><see langword="true"/> when this session id names the default (main) session — i.e. <see cref="Name"/> equals <see cref="DefaultSessionName"/>.</summary>
    public bool IsDefault => Name == DefaultSessionName;
}
