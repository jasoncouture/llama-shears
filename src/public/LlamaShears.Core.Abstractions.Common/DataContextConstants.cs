namespace LlamaShears.Core.Abstractions.Common;

/// <summary>
/// Constants used by the data-context infrastructure.
/// </summary>
public static class DataContextConstants
{
    /// <summary>
    /// DI key under which singleton <see cref="IDataContextItemProvider"/>
    /// implementations are registered. The factory consumes this key via
    /// <c>[FromKeyedServices]</c> at construction.
    /// </summary>
    public const string SingletonKey = "__data_context_item_singleton";
}
