using LlamaShears.Core.Abstractions.Agent.Sessions;

namespace LlamaShears.Core.Abstractions.Common;

/// <summary>
/// A keyed bag of arbitrary values flowing on the current call chain.
/// Backed by a stack-of-dictionaries so callers can <see cref="BeginScope"/>
/// to overlay temporary mutations without leaking them out of the using
/// block.
/// </summary>
public interface IDataContextScope : IEnumerable<KeyValuePair<string, object?>>
{
    /// <summary>The session this scope belongs to, assigned at start.</summary>
    SessionId Key { get; }

    /// <summary>
    /// Returns the value at <paramref name="key"/> if it is assignable to
    /// <typeparamref name="T"/>; otherwise <see langword="false"/> with
    /// <paramref name="value"/> set to <see langword="null"/>.
    /// </summary>
    bool TryGetValue<T>(string key, out T? value) where T : class;

    /// <summary>
    /// Pushes the current dictionary onto a stack and creates a copy as
    /// the new working set. Disposing the returned token pops back to
    /// the snapshot, discarding any mutations made while in scope.
    /// </summary>
    IDisposable BeginScope();

    /// <summary>
    /// Materializes <paramref name="provider"/>'s items into the current
    /// dictionary. Existing keys are overwritten.
    /// </summary>
    Task SetItemsAsync(IDataContextItemProvider provider, CancellationToken cancellationToken);

    /// <summary>
    /// Sets each <paramref name="items"/> pair into the current
    /// dictionary. Existing keys are overwritten.
    /// </summary>
    void SetItems(IEnumerable<KeyValuePair<string, object?>> items);
    /// <summary>
    /// Sets <paramref name="key"/> to the provided value.
    /// Existing keys are overwritten.
    /// </summary>
    public void SetItem(string key, object? value)
    {
        SetItems([new KeyValuePair<string, object?>(key, value)]);
    }

    /// <summary>Clears every entry in the current dictionary.</summary>
    void Clear();

    /// <summary>
    /// Removes the entry at <paramref name="key"/> from the current
    /// dictionary. Returns <see langword="true"/> when an entry was
    /// removed, <see langword="false"/> when no entry existed at that
    /// key.
    /// </summary>
    bool Remove(string key);
}
