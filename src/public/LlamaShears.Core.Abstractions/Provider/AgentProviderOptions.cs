using System.Text.Json;
using System.Text.Json.Nodes;

namespace LlamaShears.Core.Abstractions.Provider;

/// <summary>
/// Helpers for layering an agent's per-model JSON options blob on top
/// of host-level defaults. Used by providers that expose a strongly-typed
/// options record for their host config and a free-form
/// <c>Options</c> JSON blob in agent config.
/// </summary>
public static class AgentProviderOptions
{
    /// <summary>
    /// Layers <paramref name="agentOverride"/> on top of
    /// <paramref name="hostDefaults"/>. Object properties merge
    /// field-by-field; arrays and scalars at any leaf replace. Returns
    /// <paramref name="hostDefaults"/> verbatim when there is no
    /// override (no allocation, no parse).
    /// </summary>
    public static TOptions Resolve<TOptions>(
        TOptions hostDefaults,
        JsonElement? agentOverride,
        JsonSerializerOptions? jsonOptions = null)
        where TOptions : class, new()
    {
        ArgumentNullException.ThrowIfNull(hostDefaults);
        if (agentOverride is null || agentOverride.Value.ValueKind == JsonValueKind.Null
            || agentOverride.Value.ValueKind == JsonValueKind.Undefined)
        {
            return hostDefaults;
        }
        var hostNode = JsonSerializer.SerializeToNode(hostDefaults, jsonOptions) ?? new JsonObject();
        var overlayNode = JsonNode.Parse(agentOverride.Value.GetRawText());
        if (overlayNode is null)
        {
            return hostDefaults;
        }
        var merged = Merge(hostNode, overlayNode);
        return merged.Deserialize<TOptions>(jsonOptions) ?? hostDefaults;
    }

    /// <summary>
    /// Layers a free-form parameter dictionary on top of
    /// <paramref name="hostDefaults"/>. Each entry is treated as a JSON
    /// property with the key as the property name; merge semantics match
    /// the <see cref="JsonElement"/> overload. Returns
    /// <paramref name="hostDefaults"/> verbatim when there is no override.
    /// </summary>
    public static TOptions Resolve<TOptions>(
        TOptions hostDefaults,
        IReadOnlyDictionary<string, JsonElement>? agentOverride,
        JsonSerializerOptions? jsonOptions = null)
        where TOptions : class, new()
    {
        ArgumentNullException.ThrowIfNull(hostDefaults);
        if (agentOverride is null || agentOverride.Count == 0)
        {
            return hostDefaults;
        }
        var hostNode = JsonSerializer.SerializeToNode(hostDefaults, jsonOptions) ?? new JsonObject();
        var overlayNode = new JsonObject();
        foreach (var (key, element) in agentOverride)
        {
            overlayNode[key] = JsonNode.Parse(element.GetRawText());
        }
        var merged = Merge(hostNode, overlayNode);
        return merged.Deserialize<TOptions>(jsonOptions) ?? hostDefaults;
    }

    private static JsonNode Merge(JsonNode? @base, JsonNode overlay)
    {
        if (@base is JsonObject baseObj && overlay is JsonObject overlayObj)
        {
            var result = new JsonObject();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (key, value) in overlayObj)
            {
                if (!seen.Add(key))
                {
                    continue;
                }
                var baseKey = baseObj
                    .Select(kv => kv.Key)
                    .FirstOrDefault(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));
                var resultKey = baseKey ?? key;
                if (value is null)
                {
                    result[resultKey] = null;
                    continue;
                }
                if (baseKey is not null
                    && baseObj.TryGetPropertyValue(baseKey, out var existing)
                    && existing is not null)
                {
                    result[resultKey] = Merge(existing, value);
                }
                else
                {
                    result[resultKey] = value.DeepClone();
                }
            }
            foreach (var (key, value) in baseObj)
            {
                if (!seen.Add(key))
                {
                    continue;
                }
                result[key] = value?.DeepClone();
            }
            return result;
        }
        return overlay.DeepClone();
    }
}
