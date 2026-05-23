using System.Diagnostics;
using System.Reflection;

namespace LlamaShears.Core.Abstractions;

/// <summary>
/// Static functions for collecting Telemetry (trace/metrics/etc)
/// </summary>
public static class Telemetry
{
    /// <summary>
    /// Create an activity source with a consistent naming convention for a specific type
    /// </summary>
    /// <typeparam name="T">The type to create the activity source for</typeparam>
    /// <param name="name">Optional name override</param>
    /// <param name="tags">Optional tags</param>
    /// <returns>A ready to use ActivitySource</returns>
    public static ActivitySource CreateActivitySourceForType<T>(string? name = null, IEnumerable<KeyValuePair<string, object?>>? tags = null)
    {
        var type = typeof(T);
        var assembly = typeof(T).Assembly;
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion
             ?? assembly.GetName()?.Version?.ToString(4);
        name ??= $"{type.Namespace}.{type.Name}".Trim('.');
        return new ActivitySource(name, version, tags);
    }
}