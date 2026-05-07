using System.Collections.Immutable;
using Microsoft.Extensions.DependencyInjection;

namespace LlamaShears.Plugins;

internal sealed class ServiceCollectionSnapshot : IServiceCollectionSnapshot
{
    private readonly IServiceCollection _services;
    private ImmutableArray<ServiceDescriptor> _snapshot;

    public ServiceCollectionSnapshot(IServiceCollection services)
    {
        _services = services;
        _snapshot = [.. services];
    }

    public void AcceptChanges()
    {
        _snapshot = [.. _services];
    }

    void IDisposable.Dispose()
    {
        _services.Clear();
        foreach (var descriptor in _snapshot)
        {
            _services.Add(descriptor);
        }
    }
}
