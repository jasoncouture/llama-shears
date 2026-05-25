using System.Collections.Immutable;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Core.Abstractions.Agent;

/// <summary>
/// Bundle of inputs needed to launch a prompted sub-agent (heartbeat, cron,
/// any tick-driven transient). Captures the config, the parent session path,
/// the initial user-role turn, optional priming turns, and optional data-scope
/// entries to seed the child's context.
/// </summary>
/// <param name="Config">Agent config the child runs under.</param>
/// <param name="Id">Child session id (parent agent id + channel name).</param>
/// <param name="ParentSessionPath">Session path of the parent session the child is attached to.</param>
/// <param name="InitialPrompt">First user-role turn the child sees on boot.</param>
/// <param name="Turns">Optional priming turns appended to the child's context before the start command is published.</param>
/// <param name="ContextData">Optional data-scope entries to seed the child's data context.</param>
/// <param name="AutoStart">When <see langword="true"/> the spawner publishes the start command immediately after building the child.</param>
public record PromptedAgentStartInformation(AgentConfig Config, SessionId Id, SessionPath ParentSessionPath, ModelTurn InitialPrompt, ImmutableArray<ModelTurn>? Turns = null, ImmutableDictionary<string, object?>? ContextData = null, bool AutoStart = true)
{
    /// <summary>
    /// Builds the default sub-agent config for a named transient (e.g. <c>"heartbeat"</c> → <c>HEARTBEAT.md</c>
    /// for both the system prompt and prompt-context templates).
    /// </summary>
    /// <param name="name">Sub-agent channel name; uppercased and used as the template stem.</param>
    /// <param name="config">Parent agent's config; the returned config is a <c>with</c> overlay.</param>
    /// <returns>The overlaid config the child should run under.</returns>
    public static AgentConfig CreateDefaultSubAgentConfig(string name, AgentConfig config)
    {
        return config with
        {
            SystemPrompt = $"{name.ToUpperInvariant()}.md",
            PromptContext = $"{name.ToUpperInvariant()}.md"
        };
    }

    /// <summary>
    /// Returns a copy with any <see langword="null"/> <see cref="Turns"/> or
    /// <see cref="ContextData"/> replaced with empty collections, so consumers
    /// can iterate without null checks.
    /// </summary>
    public PromptedAgentStartInformation WithMissingPropertiesGenerated()
    {
        return this with
        {
            Turns = Turns ?? [],
            ContextData = ContextData ?? []
        };
    }
}
