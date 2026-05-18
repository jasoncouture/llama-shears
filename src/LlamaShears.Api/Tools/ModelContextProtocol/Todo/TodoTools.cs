using System.Collections.Immutable;
using System.ComponentModel;
using LlamaShears.Core.Abstractions.Agent.Todo;
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
    [Description("Appends a batch of items to the agent's TODO list. All items in the batch share the same 'done' flag; for a mixed batch, call twice. Pass an array of one to add a single item. Returns the full list as a JSON object (state, itemCount, items[]). The whole batch is rejected if any item is empty, contains newlines, or exceeds 200 characters.")]
    public async Task<TodoListResult> AddAsync(
        [Description("Items to add. Each must be non-empty, must not contain newline characters, and must not exceed 200 characters.")] string[] items,
        [Description("Set to true to record every new item as already completed. Defaults to false.")] bool done = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _storage.AddAsync(items, done, cancellationToken);
        return ToResult(result);
    }

    [McpServerTool(Name = "todo_update")]
    [Description("Applies a batch of completion-state changes. Pass an array of one to update a single item. Updates that match the current state are silently no-ops. The whole batch is rejected if any update names a missing index. Returns the full list as a JSON object.")]
    public async Task<TodoListResult> UpdateAsync(
        [Description("Updates to apply. Each entry has a 1-based 'index' and a target 'isCompleted' state.")] TodoItemUpdate[] updates,
        CancellationToken cancellationToken = default)
    {
        var result = await _storage.UpdateAsync(updates, cancellationToken);
        return ToResult(result);
    }

    [McpServerTool(Name = "todo_delete")]
    [Description("Removes a batch of items by 1-based index. Remaining items are renumbered. Pass an array of one to delete a single item. Duplicate indices are deduped; the whole batch is rejected if any index is missing. Returns the full list as a JSON object.")]
    public async Task<TodoListResult> DeleteAsync(
        [Description("1-based indices of items to delete.")] int[] indices,
        CancellationToken cancellationToken = default)
    {
        var result = await _storage.DeleteAsync(indices, cancellationToken);
        return ToResult(result);
    }

    [McpServerTool(Name = "todo_clear")]
    [Description("Removes items from the list. By default only completed items are removed; pass include_incomplete=true to also remove incomplete items (wipes everything). Returns the full list after the clear as a JSON object.")]
    public async Task<TodoListResult> ClearAsync(
        [Description("False (default) clears completed items only; true also clears incomplete items.")] bool includeIncomplete = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _storage.ClearAsync(includeIncomplete, cancellationToken);
        return ToResult(result);
    }

    [McpServerTool(Name = "todo_list")]
    [Description("Returns the current TODO list as a JSON object (state, itemCount, items[]).")]
    public async Task<TodoListResult> ListAsync(
        [Description("Number of items to skip from the start. Null or non-positive starts at the beginning.")] int? offset = null,
        [Description("Maximum number of items to return. Null or negative returns all remaining items.")] int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _storage.ListAsync(offset, limit, cancellationToken);
        return ToResult(result);
    }

    private static TodoListResult ToResult(TodoCommandResult result)
    {
        var ordered = result.Items.OrderBy(i => i.Index).ToImmutableArray();
        var error = result.State switch
        {
            TodoResultState.Success => null,
            TodoResultState.Corrupt => result.Message is null
                ? "Todo list was corrupt; a new empty list has been created."
                : $"Todo list was corrupt; a new empty list has been created: {result.Message}",
            TodoResultState.Refused => result.Message is null
                ? "Refused"
                : $"Refused: {result.Message}",
            _ => $"Unknown state: {result.State}",
        };
        return new TodoListResult(
            State: result.State,
            ItemCount: ordered.Length,
            Items: ordered,
            Error: error);
    }
}
