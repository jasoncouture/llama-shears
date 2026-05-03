using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace LlamaShears.Core.Abstractions.Events;

public sealed partial record EventType(string Component, string EventName, string? Id = null)
{
    [GeneratedRegex("^[a-z0-9_-]+:[a-z0-9_-]+(?::[a-z0-9_.-]+)*$")]
    private static partial Regex GetValidationRegex();

    [return: NotNullIfNotNull(nameof(value))]
    public static implicit operator string?(EventType? value) => value?.ToString();

    public static explicit operator EventType(string value)
    {
        if (!TryParse(value, out var typed))
        {
            throw new ArgumentException($"Invalid event type: {value}", nameof(value));
        }
        return typed;
    }

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

    public override string ToString()
        => Id is null ? $"{Component}:{EventName}" : $"{Component}:{EventName}:{Id}";
}
