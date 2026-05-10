using System.Collections.Immutable;

namespace LlamaShears.Core.Abstractions.Common;

/// <summary>
/// Convenience accessors over <see cref="IDataContextScope"/>.
/// </summary>
public static class DataContextScopeExtensions
{
    /// <summary>
    /// Returns an immutable snapshot of <paramref name="scope"/>'s current
    /// dictionary. Throws when the receiver is <see langword="null"/>; sites
    /// that snapshot the scope legitimately cannot proceed without one.
    /// Subsequent mutations to the scope do not affect the returned value.
    /// </summary>
    public static ImmutableDictionary<string, object?> Snapshot(this IDataContextScope? scope)
    {
        if (scope is null)
        {
            throw new InvalidOperationException(
                "Cannot snapshot a null data context scope; no scope is active on the current call chain.");
        }
        var builder = ImmutableDictionary.CreateBuilder<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in scope)
        {
            builder[pair.Key] = pair.Value;
        }
        return builder.ToImmutable();
    }
}
