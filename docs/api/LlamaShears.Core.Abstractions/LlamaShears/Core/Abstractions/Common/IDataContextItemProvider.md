# LlamaShears.Core.Abstractions.Common.IDataContextItemProvider

Assembly: `LlamaShears.Core.Abstractions`

Contributes key/value pairs into the current data-context scope.
Implementations should not throw; on failure, return an empty
enumerable. The factory aggregates items from every registered
provider when a context starts.

## Methods

### `GetItemsForCurrentContext`(CancellationToken cancellationToken)

Returns the key/value pairs this provider wants to add to the
current scope. Called once per `StartContextAsync`.

