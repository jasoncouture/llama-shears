using LlamaShears.Core.Abstractions.Agent.Todo;
using LlamaShears.Core.Abstractions.Common;

namespace LlamaShears.Core.Todo;

internal sealed class TodoListDataProvider : IDataContextItemProvider
{
    private readonly ITodoStorage _storage;

    public TodoListDataProvider(ITodoStorage storage)
    {
        _storage = storage;
    }

    public async Task<IEnumerable<KeyValuePair<string, object?>>> GetItemsForCurrentContext(
        CancellationToken cancellationToken = default)
    {
        var result = await _storage.ListAsync(cancellationToken: cancellationToken);
        return [new KeyValuePair<string, object?>(TodoStorageConstants.DataKey, result.Items)];
    }
}
