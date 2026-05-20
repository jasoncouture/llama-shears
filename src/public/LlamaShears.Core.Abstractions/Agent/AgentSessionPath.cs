using System.Collections.Immutable;
using System.Text;

namespace LlamaShears.Core;

/// <summary>
/// Materialised parent-chain of session ids for an agent, ordered root-last. Built lazily by
/// the repository when full ancestry is needed for logging or routing.
/// </summary>
/// <param name="Segments">Session ids in current-to-root order.</param>
public sealed record AgentSessionPath(ImmutableArray<Guid> Segments)
{
    private string? _cachedString;

    /// <inheritdoc/>
    public override string ToString()
    {
        return _cachedString ??= BuildPathString();
    }

    private string BuildPathString()
    {
        if (Segments.IsDefaultOrEmpty) return string.Empty;
        StringBuilder builder = new StringBuilder();
        foreach (var segment in Segments.Reverse())
        {
            builder.Append('/').Append(segment);
        }

        return builder.ToString();
    }

    /// <summary>Number of ancestor hops from this session to the root.</summary>
    public int Depth
    {
        get
        {
            if (Segments.IsDefaultOrEmpty)
                throw new InvalidOperationException("There are no segments, this should not happen!");
            return Segments.Length - 1;
        }
    }
}
