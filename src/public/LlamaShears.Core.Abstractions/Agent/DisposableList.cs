namespace LlamaShears.Core;

/// <summary>
/// Composite disposable that owns a LIFO stack of mixed <see cref="IDisposable"/> and
/// <see cref="IAsyncDisposable"/> instances. Disposal walks the stack in reverse order,
/// catches per-item exceptions, and rethrows as an <see cref="AggregateException"/>.
/// Sync <see cref="IDisposable.Dispose"/> blocks on the async path — async-first by design.
/// </summary>
public class DisposableList : IDisposable, IAsyncDisposable
{
    private readonly Stack<object> _disposables = new Stack<object>();

    internal DisposableList Push(IDisposable? disposable) => PushInternal(disposable);
    internal DisposableList Push(IAsyncDisposable? disposable) => PushInternal(disposable);
    private DisposableList PushInternal(object? obj)
    {
        if (obj is not null) _disposables.Push(obj);
        return this;
    }

    void IDisposable.Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();

    /// <summary>Disposes every entry in reverse insertion order, aggregating any thrown exceptions.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposables.Count == 0) return;
        var exceptions = new List<Exception>();
        while (_disposables.Count > 0)
        {
            var item = _disposables.Pop();
            try
            {
                if (item is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync();
                }
                else if (item is IDisposable disposable)
                {
                    disposable.Dispose();
                }
                else
                {
                    throw new InvalidOperationException("What the fuck, this is a bug, how the fuck did you do this?");
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }

        if (exceptions.Count > 0) throw new AggregateException(exceptions);
    }

    /// <summary>Allocates a new, empty <see cref="DisposableList"/>.</summary>
    public static DisposableList Create()
    {
        return new DisposableList();
    }
}
