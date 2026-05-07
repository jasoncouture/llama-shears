namespace LlamaShears.Plugins.Host;

/// <summary>
/// Reversible record of an <c>IServiceCollection</c>'s descriptor list.
/// Disposing without calling <see cref="AcceptChanges"/> rolls the
/// collection back to the state captured at snapshot time; calling
/// <see cref="AcceptChanges"/> commits the current state as the new
/// rollback baseline.
/// </summary>
public interface IServiceCollectionSnapshot : IDisposable
{
    /// <summary>
    /// Replaces the snapshot baseline with the underlying collection's
    /// current state. After this call, disposal will not roll back any
    /// changes made up to this point.
    /// </summary>
    public void AcceptChanges();

    /// <summary>
    /// Restores the underlying collection to the captured baseline.
    /// Equivalent to <see cref="IDisposable.Dispose"/>.
    /// </summary>
    public void Rollback() => Dispose();
}
