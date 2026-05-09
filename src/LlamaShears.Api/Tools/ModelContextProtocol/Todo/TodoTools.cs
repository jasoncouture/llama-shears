using System.ComponentModel;
using ModelContextProtocol.Server;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Todo;

[McpServerToolType]
public sealed class TodoTools
{
    private readonly ITodoStorage _storage;

    public TodoTools(ITodoStorage storage)
    {
        _storage = storage;
    }

    [McpServerTool(Name = "todo_add")]
    [Description("Appends a new item to the agent's TODO list. Returns the full list after the addition.")]
    public async Task<string> AddAsync(
        [Description("Item text. Must not contain newline characters. Must not exceed 200 characters.")] string text,
        [Description("Set to true to record the new item as already completed. Defaults to false.")] bool done = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _storage.AddAsync(text, done, cancellationToken).ConfigureAwait(false);
        return result.ToString();
    }

    [McpServerTool(Name = "todo_update")]
    [Description("Toggles the completion state of the item at the given 1-based index. No-ops when the state already matches; refuses when no item exists at the index.")]
    public async Task<string> UpdateAsync(
        [Description("1-based index of the item to update.")] int index,
        [Description("Target completion state.")] bool isCompleted,
        CancellationToken cancellationToken = default)
    {
        var result = await _storage.UpdateAsync(index, isCompleted, cancellationToken).ConfigureAwait(false);
        return result.ToString();
    }

    [McpServerTool(Name = "todo_delete")]
    [Description("Removes the item at the given 1-based index. Remaining items are renumbered. Returns the full list after the deletion.")]
    public async Task<string> DeleteAsync(
        [Description("1-based index of the item to delete.")] int index,
        CancellationToken cancellationToken = default)
    {
        var result = await _storage.DeleteAsync(index, cancellationToken).ConfigureAwait(false);
        return result.ToString();
    }

    [McpServerTool(Name = "todo_clear")]
    [Description("Removes items from the list. By default only completed items are removed; pass include_completed=true to wipe everything.")]
    public async Task<string> ClearAsync(
        [Description("False (default) clears completed items only; true clears every item.")] bool includeCompleted = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _storage.ClearAsync(includeCompleted, cancellationToken).ConfigureAwait(false);
        return result.ToString();
    }

    [McpServerTool(Name = "todo_list")]
    [Description("Returns the current TODO list as numbered checkbox lines. Returns 'No todo items found.' when empty.")]
    public async Task<string> ListAsync(
        [Description("Number of items to skip from the start. Null or non-positive starts at the beginning.")] int? offset = null,
        [Description("Maximum number of items to return. Null or negative returns all remaining items.")] int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _storage.ListAsync(offset, limit, cancellationToken).ConfigureAwait(false);
        var rendered = result.ToString();
        return string.IsNullOrEmpty(rendered) ? "No todo items found." : rendered;
    }
}
