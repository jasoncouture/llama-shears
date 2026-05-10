namespace LlamaShears.Core.Abstractions.Common;

/// <summary>
/// Contributes key/value pairs into the current data-context scope.
/// Implementations should not throw; on failure, return an empty
/// enumerable. The factory aggregates items from every registered
/// provider when a context starts.
/// </summary>
public interface IDataContextItemProvider
{
    /// <summary>
    /// Returns the key/value pairs this provider wants to add to the
    /// current scope. Called once per <c>StartContextAsync</c>.
    /// </summary>
    Task<IEnumerable<KeyValuePair<string, object?>>> GetItemsForCurrentContext(CancellationToken cancellationToken = default);
}
