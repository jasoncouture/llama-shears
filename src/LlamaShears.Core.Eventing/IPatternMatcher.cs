using LlamaShears.Core.Abstractions.Events;

namespace LlamaShears.Core.Eventing;

/// <summary>
/// Matches an <see cref="EventType"/> against a subscription pattern.
/// Implementations are expected to cache compiled forms internally so that
/// repeated dispatches against the same pattern do not pay parse/compile
/// cost on every call.
/// </summary>
public interface IPatternMatcher
{
    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="type"/>'s wire
    /// form satisfies <paramref name="pattern"/>.
    /// </summary>
    bool IsMatch(string pattern, EventType type);
}
