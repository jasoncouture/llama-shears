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

    private static JsonNode Merge(JsonNode? @base, JsonNode overlay)
    {
        if (@base is JsonObject baseObj && overlay is JsonObject overlayObj)
        {
            foreach (var (key, value) in overlayObj.ToList())
            {
                if (value is null)
                {
                    baseObj[key] = null;
                    continue;
                }
                overlayObj.Remove(key);
                if (baseObj.TryGetPropertyValue(key, out var existing) && existing is not null)
                {
                    baseObj[key] = Merge(existing, value);
                }
                else
                {
                    baseObj[key] = value;
                }
            }
            return baseObj;
        }
        return overlay;
    }
}
