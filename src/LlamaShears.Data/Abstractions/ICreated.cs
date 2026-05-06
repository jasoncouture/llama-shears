namespace LlamaShears.Data.Abstractions;

/// <summary>
/// Marker for an entity that records the timestamp at which it was
/// first persisted. Set unconditionally on add by the timestamp
/// interceptor and immutable afterwards.
/// </summary>
public interface ICreated
{
    /// <summary>
    /// Timestamp at which the entity was first persisted.
    /// </summary>
    DateTimeOffset Created { get; init; }
}
