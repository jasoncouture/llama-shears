namespace LlamaShears.Core;

/// <summary>
/// Extension methods that chain disposables into a single <see cref="DisposableList"/>.
/// Folds the resulting list when either side is already a list, so a chain of
/// <c>.And(x).And(y).And(z)</c> stays flat.
/// </summary>
public static class CombinedDisposable
{
    /// <summary>Combines two synchronous disposables.</summary>
    public static DisposableList And(this IDisposable current, IDisposable? disposable)
    {
        if (current is DisposableList list) return list.Push(disposable);
        return new DisposableList().Push(current).Push(disposable);
    }

    /// <summary>Combines a synchronous disposable with an asynchronous one.</summary>
    public static DisposableList And(this IDisposable current, IAsyncDisposable? disposable)
    {
        if (current is DisposableList list) return list.Push(disposable);
        return new DisposableList().Push(current).Push(disposable);
    }

    /// <summary>Combines an asynchronous disposable with a synchronous one.</summary>
    public static DisposableList And(this IAsyncDisposable current, IDisposable? disposable)
    {
        if (current is DisposableList list) return list.Push(disposable);
        return new DisposableList().Push(current).Push(disposable);
    }

    /// <summary>Combines two asynchronous disposables.</summary>
    public static DisposableList And(this IAsyncDisposable current, IAsyncDisposable? disposable)
    {
        if (current is DisposableList list) return list.Push(disposable);
        return new DisposableList().Push(current).Push(disposable);
    }

    /// <summary>Appends a synchronous disposable to an existing list.</summary>
    public static DisposableList And(this DisposableList current, IDisposable? disposable)
    {
        return current.Push(disposable);
    }

    /// <summary>Appends an asynchronous disposable to an existing list.</summary>
    public static DisposableList And(this DisposableList current, IAsyncDisposable? disposable)
    {
        return current.Push(disposable);
    }
}
