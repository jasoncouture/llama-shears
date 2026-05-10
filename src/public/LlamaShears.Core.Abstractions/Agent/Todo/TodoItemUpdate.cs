namespace LlamaShears.Core.Abstractions.Agent.Todo;

/// <summary>
/// Patch applied to a single <see cref="TodoItem"/> when the agent toggles
/// its completion state.
/// </summary>
/// <param name="Index">1-based ordinal of the item to update; matches <see cref="TodoItem.Index"/>.</param>
/// <param name="IsCompleted">Target completion state for the addressed item.</param>
public sealed record TodoItemUpdate(int Index, bool IsCompleted);
