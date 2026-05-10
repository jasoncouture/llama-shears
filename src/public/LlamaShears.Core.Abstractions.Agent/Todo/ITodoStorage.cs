namespace LlamaShears.Core.Abstractions.Agent.Todo;

/// <summary>
/// Persists the agent's TODO list as a Markdown file at the workspace
/// root. All mutations rewrite or append to that file; a corrupt file
/// is reset to the canonical empty state and the result reflects that
/// recovery.
/// </summary>
public interface ITodoStorage
{
    /// <summary>
    /// Removes items from the list. By default only completed items are
    /// removed; when <paramref name="includeIncomplete"/> is
    /// <see langword="true"/> incomplete items are also removed,
    /// effectively wiping the list.
    /// </summary>
    /// <param name="includeIncomplete">
    /// <see langword="false"/> (default) clears completed items only.
    /// <see langword="true"/> also clears incomplete items.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask<TodoCommandResult> ClearAsync(bool includeIncomplete, CancellationToken cancellationToken = default);

    /// <summary>
    /// Appends a batch of items to the list with fresh sequential indices.
    /// All items in <paramref name="items"/> share the same
    /// <paramref name="done"/> flag; for a mixed batch, call twice.
    /// </summary>
    /// <param name="items">
    /// Item texts. Each must be non-empty, must not contain newline
    /// characters, and must not exceed the configured maximum length.
    /// The whole batch is rejected if any item is invalid.
    /// </param>
    /// <param name="done">
    /// <see langword="true"/> records every new item as already completed.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask<TodoCommandResult> AddAsync(IReadOnlyList<string> items, bool done = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies a batch of completion-state changes. The whole batch is
    /// rejected if any update names a missing index. Updates that match
    /// the current state are silently no-ops; the rewrite still happens
    /// only when at least one change took effect.
    /// </summary>
    /// <param name="updates">1-based index plus the target state for each item.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask<TodoCommandResult> UpdateAsync(IReadOnlyList<TodoItemUpdate> updates, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a batch of items by 1-based index and renumbers the
    /// remainder. Duplicate indices are deduped. The whole batch is
    /// rejected if any index is missing.
    /// </summary>
    /// <param name="indices">1-based indices of items to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask<TodoCommandResult> DeleteAsync(IReadOnlyList<int> indices, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the current list, optionally paginated.
    /// </summary>
    /// <param name="offset">
    /// Number of items to skip from the start. <see langword="null"/>
    /// or non-positive starts at the beginning.
    /// </param>
    /// <param name="limit">
    /// Maximum number of items to return. <see langword="null"/> or
    /// negative returns all remaining items.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask<TodoCommandResult> ListAsync(int? offset = default, int? limit = default, CancellationToken cancellationToken = default);
}
