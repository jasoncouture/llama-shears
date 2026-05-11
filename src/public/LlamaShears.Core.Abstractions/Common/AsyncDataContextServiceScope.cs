using Microsoft.Extensions.DependencyInjection;

namespace LlamaShears.Core.Abstractions.Common;

/// <summary>
/// Decorator that owns an <see cref="AsyncServiceScope"/> alongside an
/// <see cref="IDisposable"/> data-context frame. Disposing the decorator
/// tears down the data frame and the DI scope as one operation.
/// </summary>
public readonly struct AsyncDataContextServiceScope : IAsyncDisposable
{
    private readonly AsyncServiceScope _serviceScope;
    private readonly IDisposable _dataFrame;

    internal AsyncDataContextServiceScope(AsyncServiceScope serviceScope, IDisposable dataFrame)
    {
        _serviceScope = serviceScope;
        _dataFrame = dataFrame;
    }

    /// <summary>The DI service scope's provider.</summary>
    public IServiceProvider ServiceProvider => _serviceScope.ServiceProvider;

    /// <summary>The underlying DI service scope.</summary>
    public IServiceScope ServiceScope => _serviceScope;

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        _dataFrame.Dispose();
        await _serviceScope.DisposeAsync();
    }
}
