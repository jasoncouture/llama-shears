namespace LlamaShears.Core.Abstractions.Common;

/// <summary>
/// Marker interface. Values implementing this survive a
/// <see cref="IDataContextScope.BeginScope"/> pop: when the inner scope
/// is disposed, any key whose current value implements this marker is
/// copied into the parent dictionary before the parent is restored as
/// the working set.
/// </summary>
public interface IPersistentDataContextItem;
