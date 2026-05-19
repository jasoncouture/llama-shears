namespace LlamaShears.Core.Abstractions.Agent.Sessions;

/// <summary>
/// Extensions that overlay an <see cref="IAgentData"/>'s entries onto a target dictionary.
/// </summary>
public static class SessionDataExtensions
{
    /// <summary>
    /// Writes every entry from <paramref name="data"/>'s <see cref="IAgentData.GetData"/> into
    /// <paramref name="state"/>, replacing any existing values under the same keys.
    /// </summary>
    public static void ApplyTo(this IAgentData data, IDictionary<string, object?> state)
    {
        foreach (var (key, value) in data.GetData())
        {
            state[key] = value;
        }
    }
}
