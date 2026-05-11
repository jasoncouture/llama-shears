using Microsoft.Extensions.DependencyInjection;

namespace LlamaShears.Core.Abstractions.Common;

/// <summary>
/// Composition helpers that pair a DI <see cref="IServiceScope"/> with
/// a new <see cref="IDataContextScope.BeginScope"/> frame so the two
/// share a single disposable lifetime.
/// </summary>
public static class DataContextScopeFactoryExtensions
{
    /// <summary>
    /// Creates an <see cref="AsyncServiceScope"/> and opens a new
    /// <see cref="IDataContextScope.BeginScope"/> frame on the data scope
    /// resolved from the new DI scope. The returned bundle disposes both
    /// (data frame first, then the DI scope) when disposed.
    /// </summary>
    public static AsyncDataContextServiceScope CreateAsyncScopeWithData(this IServiceScopeFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        var serviceScope = factory.CreateAsyncScope();
        var dataScope = serviceScope.ServiceProvider.GetRequiredService<IDataContextScope>();
        var dataFrame = dataScope.BeginScope();
        return new AsyncDataContextServiceScope(serviceScope, dataFrame);
    }

    /// <summary>
    /// Resolves every scoped <see cref="IDataContextItemProvider"/>
    /// registered against <paramref name="scope"/>'s provider and
    /// applies each to the <see cref="IDataContextScope"/> resolved
    /// from the same provider.
    /// </summary>
    public static async Task ApplyScopeDataAsync(this IServiceScope scope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scope);
        var dataScope = scope.ServiceProvider.GetRequiredService<IDataContextScope>();
        foreach (var provider in scope.ServiceProvider.GetScopedDataProviders())
        {
            await dataScope.SetItemsAsync(provider, cancellationToken);
        }
    }
}

