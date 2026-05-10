using LlamaShears.Core.Abstractions.Common;

namespace LlamaShears.Core.Abstractions.Provider;

/// <summary>
/// Convenience accessors for pulling the active <see cref="ModelConfiguration"/>
/// off an <see cref="IDataContextScope"/> without callers having to remember
/// the well-known key.
/// </summary>
public static class ModelConfigurationExtensions
{
    /// <summary>
    /// Returns the <see cref="ModelConfiguration"/> attached to the given scope
    /// under <see cref="ModelConfiguration.DataKey"/>, or <see langword="null"/>
    /// if none is set.
    /// </summary>
    /// <param name="scope">Data-context scope to inspect.</param>
    /// <returns>The active model configuration, or <see langword="null"/> when the scope has none.</returns>
    public static ModelConfiguration? GetModelConfiguration(this IDataContextScope scope)
    {
        scope.TryGetValue<ModelConfiguration>(ModelConfiguration.DataKey, out var config);
        return config;
    }
}
