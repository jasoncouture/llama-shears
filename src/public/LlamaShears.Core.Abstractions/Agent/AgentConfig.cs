using System.Collections.Immutable;
using System.Text.Json.Serialization;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Core.Abstractions.Agent;

/// <summary>
/// Immutable on-disk configuration snapshot for one agent. Loaded from
/// <c>&lt;Data&gt;/agents/&lt;id&gt;.json</c> by <see cref="IAgentConfigProvider"/>
/// and held for the duration of an in-flight interaction so a single
/// turn sees one consistent configuration end-to-end.
/// </summary>
/// <param name="Model">Language model selection and per-call options.</param>
/// <param name="Id">Stable agent identifier; populated from the file name and not serialized back into the JSON body.</param>
/// <param name="Hash">Content hash of the on-disk config file bytes, computed before deserialization. Used as a change token for concurrent-edit detection; not serialized into the JSON body.</param>
/// <param name="WorkspacePath">Absolute or workspace-relative path to the agent's workspace overlay; <see langword="null"/> falls back to the framework default.</param>
/// <param name="SystemPrompt">File name (including extension) of the system-prompt template to render, e.g. <c>DEFAULT.md</c>; <see langword="null"/> uses <c>DEFAULT.md</c>.</param>
/// <param name="PromptContext">Name of the per-turn prompt-context template; <see langword="null"/> uses <c>PROMPT</c>.</param>
/// <param name="Embedding">Embedding model selection used for memory search; <see langword="null"/> disables memory features.</param>
/// <param name="ModelContextProtocolServers">Set of MCP server names this agent is allowed to call; <see langword="null"/> grants no MCP access.</param>
public sealed record AgentConfig(
    [property: JsonRequired] ModelConfiguration Model,
    [property: JsonIgnore] string Id = "",
    [property: JsonIgnore] string Hash = "",
    string? WorkspacePath = null,
    string? SystemPrompt = null,
    string? PromptContext = null,
    ModelConfiguration? Embedding = null,
    [property: JsonPropertyName("mcpServers")] ImmutableHashSet<string>? ModelContextProtocolServers = null) : IAgentData
{
    /// <summary>How often the host injects a heartbeat turn into an idle agent. Defaults to 30 minutes.</summary>
    public TimeSpan HeartbeatPeriod { get; init; } = TimeSpan.FromMinutes(30);
    /// <summary>Tool-loop budget and related guardrails for this agent.</summary>
    public AgentToolConfig Tools { get; init; } = new AgentToolConfig();
    /// <summary>Memory-subsystem options (e.g. eager prefetch).</summary>
    public AgentMemoryConfig Memory { get; init; } = new AgentMemoryConfig();
    /// <summary>Key used to stash the active <see cref="AgentConfig"/> in the per-turn data context scope.</summary>
    public const string DataKey = "agent_configuration";

    /// <inheritdoc />
    public IEnumerable<KeyValuePair<string, object?>> GetData()
    {
        yield return new KeyValuePair<string, object?>(DataKey, this);
    }
}
