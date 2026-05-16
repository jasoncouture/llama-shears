namespace LlamaShears.Core.Abstractions.Agent.Todo;

/// <summary>
/// Well-known keys for the TODO subsystem.
/// </summary>
public static class TodoStorageConstants
{
    /// <summary>
    /// Data-context key under which the current agent's TODO items
    /// (<see cref="System.Collections.Immutable.ImmutableArray{TodoItem}"/>)
    /// flow on the active scope.
    /// </summary>
    public const string DataKey = "todo";
}
