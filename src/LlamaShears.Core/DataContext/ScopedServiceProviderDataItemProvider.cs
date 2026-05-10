using System.Collections.Immutable;
using LlamaShears.Core.Abstractions.Common;

namespace LlamaShears.Core.DataContext;

public sealed class ScopedServiceProviderDataItemProvider : IDataContextItemProvider
{
    private readonly ImmutableArray<KeyValuePair<string, object?>> _items;

    public ScopedServiceProviderDataItemProvider(IServiceProvider serviceProvider)
    {
        _items = [new KeyValuePair<string, object?>("service_provider", serviceProvider)];
    }

    public Task<IEnumerable<KeyValuePair<string, object?>>> GetItemsForCurrentContext(CancellationToken cancellationToken = default)
        => Task.FromResult<IEnumerable<KeyValuePair<string, object?>>>(_items);
}
