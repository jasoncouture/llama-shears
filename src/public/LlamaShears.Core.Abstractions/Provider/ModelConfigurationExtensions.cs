using System.Text.Json;

using LlamaShears.Core.Abstractions.Common;

namespace LlamaShears.Core.Abstractions.Provider;

/// <summary>
/// Convenience accessors for pulling the active <see cref="ModelConfiguration"/>
/// off an <see cref="IDataContextScope"/> without callers having to remember
/// the well-known key, plus a flat-schema projection helper for handing the
/// configuration to a provider-specific options record.
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
    public static ModelConfiguration? TryGetModelConfiguration(this IDataContextScope? scope)
    {
        if (scope is null) return null;
        scope.TryGetValue<ModelConfiguration>(ModelConfiguration.DataKey, out var modelConfiguration);
        return modelConfiguration;
    }
    
    /// <summary>
    /// Returns the <see cref="ModelConfiguration"/> attached to the given scope
    /// under <see cref="ModelConfiguration.DataKey"/>. Throws when the scope is
    /// <see langword="null"/> or has no model configuration stashed; intended
    /// for sites that legitimately cannot proceed without one.
    /// </summary>
    public static ModelConfiguration GetModelConfiguration(this IDataContextScope? scope)
    {
        var modelConfiguration = scope.TryGetModelConfiguration() ?? throw new InvalidOperationException("Model configuration is not available in the current data context, or the data context is null");
        return modelConfiguration;
    }

    /// <summary>
    /// Projects <paramref name="configuration"/> onto the flat JSON schema
    /// (known fields + spread <see cref="ModelConfiguration.Parameters"/>) and
    /// rehydrates as <typeparamref name="T"/>. Useful for handing the config
    /// to a provider-specific options record without re-doing the field copy.
    /// </summary>
    public static T ConvertTo<T>(this ModelConfiguration configuration, JsonSerializerOptions? options = null)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var json = JsonSerializer.SerializeToUtf8Bytes(configuration, options);
        return JsonSerializer.Deserialize<T>(json, options)
               ?? throw new InvalidOperationException(
                   $"Failed to hydrate {typeof(T).Name} from ModelConfiguration JSON.");
    }

    /// <summary>
    /// Reads <paramref name="key"/> from <see cref="ModelConfiguration.Parameters"/>
    /// and deserializes the captured <see cref="JsonElement"/> as
    /// <typeparamref name="T"/>.
    /// </summary>
    /// <returns>
    /// <see langword="false"/> when the key is absent or deserialization fails.
    /// When the captured value is JSON <c>null</c>: <see langword="true"/> with
    /// <paramref name="value"/> set to <see langword="null"/> if
    /// <typeparamref name="T"/> can hold null (reference type or
    /// <see cref="Nullable{T}"/>), <see langword="false"/> otherwise.
    /// </returns>
    public static bool TryGetValue<T>(
        this ModelConfiguration configuration,
        string key,
        out T? value,
        JsonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrEmpty(key);
        value = default;
        if (configuration.Parameters is null || !configuration.Parameters.TryGetValue(key, out var element))
        {
            return false;
        }
        if (element.ValueKind == JsonValueKind.Null)
        {
            return default(T) is null;
        }
        try
        {
            value = element.Deserialize<T>(options);
            return true;
        }
        catch (JsonException)
        {
            value = default;
            return false;
        }
    }
}
