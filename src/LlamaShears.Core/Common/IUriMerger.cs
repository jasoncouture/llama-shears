namespace LlamaShears.Core.Common;

/// <summary>
/// Combines a root URI with a second URI by preserving the root's
/// scheme, host, port, and base path while appending the second URI's
/// path tail and merging its query string. Query keys present on the
/// root URI take precedence over duplicates on the second URI.
/// </summary>
public interface IUriMerger
{
    /// <summary>
    /// Merges <paramref name="otherUri"/> into <paramref name="rootUri"/>:
    /// scheme/host/port/userinfo and the root's base path come from
    /// <paramref name="rootUri"/>; any non-root path on
    /// <paramref name="otherUri"/> is appended; the query strings are
    /// merged with root-wins semantics on duplicate keys. When
    /// <paramref name="otherUri"/> contributes neither path nor query,
    /// <paramref name="rootUri"/> is returned unchanged.
    /// </summary>
    Uri Merge(Uri rootUri, Uri otherUri);
}
