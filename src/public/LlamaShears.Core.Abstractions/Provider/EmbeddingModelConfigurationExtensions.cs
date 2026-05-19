using System.Text.Json;

namespace LlamaShears.Core.Abstractions.Provider;

/// <summary>
/// Convenience accessors for the asymmetric-embedding prefix knobs that
/// agents stash inside <see cref="ModelConfiguration.Parameters"/>. The
/// values are provider-specific, so they ride the free-form parameter
/// dictionary rather than earning dedicated record fields.
/// </summary>
public static class EmbeddingModelConfigurationExtensions
{
    /// <summary>Parameter key for the asymmetric query prefix.</summary>
    public const string QueryPrefixKey = "queryPrefix";

    /// <summary>Parameter key for the asymmetric document prefix.</summary>
    public const string DocumentPrefixKey = "documentPrefix";

    /// <summary>Returns the configured query prefix, or <see langword="null"/> when absent or non-string.</summary>
    public static string? GetQueryPrefix(this ModelConfiguration configuration)
        => GetStringParameter(configuration, QueryPrefixKey);

    /// <summary>Returns the configured document prefix, or <see langword="null"/> when absent or non-string.</summary>
    public static string? GetDocumentPrefix(this ModelConfiguration configuration)
        => GetStringParameter(configuration, DocumentPrefixKey);

    private static string? GetStringParameter(ModelConfiguration configuration, string key)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        if (configuration.Parameters is null || !configuration.Parameters.TryGetValue(key, out var element))
        {
            return null;
        }
        return element.ValueKind == JsonValueKind.String ? element.GetString() : null;
    }
}
