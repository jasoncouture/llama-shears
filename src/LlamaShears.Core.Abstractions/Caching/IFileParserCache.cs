namespace LlamaShears.Core.Abstractions.Caching;

/// <summary>
/// Read-through cache for parsing on-disk files. Wraps an
/// <see cref="IShearsCache{T}"/> so the owning <typeparamref name="T"/>
/// scopes the keyspace exactly as it does for direct cache use.
/// <para>
/// Each call computes a key from the file's path, existence,
/// last-write time, and length, prefixed with the requested
/// <c>TItem</c> type. When the key hits, the cached value is returned
/// without invoking the parser. On miss, the parser receives the open
/// file stream — or <see langword="null"/> when the file does not
/// exist — and its result is cached under that key.
/// </para>
/// <para>
/// Because the key folds in mtime and length, edits to the file
/// produce a new key on the next call and the previous entry ages out
/// naturally; callers do not invalidate by hand.
/// </para>
/// </summary>
/// <typeparam name="T">The owning type. Used as the key prefix.</typeparam>
public interface IFileParserCache<T> where T : class
{
    /// <summary>
    /// Returns the parsed value for <paramref name="path"/>, invoking
    /// <paramref name="parser"/> on miss. When the file is missing the
    /// parser is called with a <see langword="null"/> stream and may
    /// return <see langword="null"/> to signal "no value"; that null
    /// is cached too. <paramref name="state"/> is forwarded to the
    /// parser unchanged so callers can avoid a closure allocation.
    /// </summary>
    ValueTask<TItem?> GetOrParseAsync<TItem, TState>(
        string path,
        TState state,
        Func<Stream?, TState, CancellationToken, ValueTask<TItem?>> parser,
        CancellationToken cancellationToken)
        where TItem : class;
}
