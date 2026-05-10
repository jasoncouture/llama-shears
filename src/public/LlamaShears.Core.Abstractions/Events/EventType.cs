using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace LlamaShears.Core.Abstractions.Events;

/// <summary>
/// Hierarchical event identifier of the form
/// <c>component:eventName[:id]</c>. The optional third segment carries
/// a per-instance discriminator so subscribers can pattern-match on
/// the prefix while keeping correlation in the suffix.
/// </summary>
/// <param name="Component">Coarse source group, e.g. <c>"agent"</c> or <c>"system"</c>.</param>
/// <param name="EventName">Event name within <paramref name="Component"/>.</param>
/// <param name="Id">Optional per-instance discriminator (e.g. agent id, channel id). <see langword="null"/> = no discriminator.</param>
public sealed partial record EventType(string Component, string EventName, string? Id = null)
{
    [GeneratedRegex("^[a-z0-9_-]+:[a-z0-9_-]+(?::[a-z0-9_.-]+)*$")]
    private static partial Regex GetValidationRegex();

    /// <summary>Implicit conversion to the canonical string form (or <see langword="null"/> for a null receiver).</summary>
    [return: NotNullIfNotNull(nameof(value))]
    public static implicit operator string?(EventType? value) => value?.ToString();

    /// <summary>
    /// Parses <paramref name="value"/> in the canonical string form.
    /// Throws <see cref="ArgumentException"/> when the input is not a
    /// valid event type — use <see cref="TryParse"/> for non-throwing
    /// parsing.
    /// </summary>
    public static explicit operator EventType(string value)
    {
        if (!TryParse(value, out var typed))
        {
            throw new ArgumentException($"Invalid event type: {value}", nameof(value));
        }
        return typed;
    }

    /// <summary>
    /// Attempts to parse <paramref name="eventType"/>. Returns
    /// <see langword="true"/> and assigns <paramref name="typed"/> on
    /// success; otherwise <see langword="false"/> with
    /// <paramref name="typed"/> set to <see langword="null"/>.
    /// </summary>
    public static bool TryParse(string eventType, [NotNullWhen(true)] out EventType? typed)
    {
        typed = null;
        if (string.IsNullOrWhiteSpace(eventType)) return false;
        if (eventType.Length > 512) return false;
        if (!GetValidationRegex().IsMatch(eventType)) return false;
        var parts = eventType.Split(':', 3, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 2) return false;
        typed = new EventType(parts[0], parts[1], parts.Length > 2 ? parts[2] : null);
        return true;
    }

    /// <inheritdoc/>
    public override string ToString()
        => Id is null ? $"{Component}:{EventName}" : $"{Component}:{EventName}:{Id}";
}
