namespace LlamaShears.Data.Abstractions;

/// <summary>
/// Marker for an entity that records the timestamp of the most
/// recent persisted change. Set unconditionally on add and on update
/// by the timestamp interceptor.
/// </summary>
public interface ILastModified
{
    /// <summary>
    /// Timestamp of the most recent persisted change.
    /// </summary>
    DateTimeOffset LastModified { get; set; }
}
