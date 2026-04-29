namespace LlamaShears.Data.Abstractions;

/// <summary>
/// Marker for an entity that has a stable, single-column identity.
/// Entities with composite keys (e.g. join tables) deliberately do
/// not implement this interface.
/// </summary>
public interface IDataObject
{
    /// <summary>
    /// Stable identity for the entity. UUIDv7 is generated on add when
    /// left as <see cref="Guid.Empty"/>. Immutable once persisted.
    /// </summary>
    Guid Id { get; init; }
}
