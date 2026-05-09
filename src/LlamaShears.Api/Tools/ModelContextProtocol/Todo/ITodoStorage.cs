namespace LlamaShears.Api.Tools.ModelContextProtocol.Todo;

/// <summary>
/// Persists the agent's TODO list as a Markdown file at the workspace
/// root. All mutations rewrite or append to that file; a corrupt file
/// is reset to the canonical empty state and the result reflects that
/// recovery.
/// </summary>
public interface ITodoStorage
{
    /// <summary>
    /// Removes items from the list. When <paramref name="includeCompleted"/>
    /// is <see langword="false"/> only completed items are removed; when
    /// <see langword="true"/> all items are removed.
    /// </summary>
    /// <param name="includeCompleted">
    /// <see langword="false"/> (default) clears completed items only.
    /// <see langword="true"/> clears every item.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask<TodoCommandResult> ClearAsync(bool includeCompleted, CancellationToken cancellationToken = default);

    /// <summary>
    /// Appends a new item to the list with a fresh sequential index.
    /// </summary>
    /// <param name="text">
    /// Item text. Must not contain newline characters and must not exceed
    /// the configured maximum length.
    /// </param>
    /// <param name="done">
    /// <see langword="true"/> records the new item as already completed.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask<TodoCommandResult> AddAsync(string text, bool done = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Toggles the completion state of the item at <paramref name="index"/>.
    /// No-ops when the item already matches <paramref name="isCompleted"/>;
    /// refuses when no item exists at the given index.
    /// </summary>
    /// <param name="index">1-based index of the item to update.</param>
    /// <param name="isCompleted">Target completion state.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask<TodoCommandResult> UpdateAsync(int index, bool isCompleted, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the item at <paramref name="index"/>. Remaining items are
    /// renumbered to preserve a contiguous 1-based sequence. Refuses when
    /// no item exists at the given index.
    /// </summary>
    /// <param name="index">1-based index of the item to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask<TodoCommandResult> DeleteAsync(int index, CancellationToken cancellationToken = default);

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
