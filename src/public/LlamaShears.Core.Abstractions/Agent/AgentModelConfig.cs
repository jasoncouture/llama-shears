using System.Text.Json;
using System.Text.Json.Serialization;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Core.Abstractions.Agent;

/// <summary>
/// Per-agent language-model selection and per-call options.
/// </summary>
/// <param name="Id">Provider/model identifier of the language model.</param>
/// <param name="Think">How aggressively a thinking-capable model should reason; <see cref="ThinkLevel.None"/> disables thinking.</param>
/// <param name="ContextLength">Override for the model's context-window size; <see langword="null"/> uses provider default.</param>
/// <param name="KeepAlive">Provider-specific keep-alive for the model; <see langword="null"/> uses provider default.</param>
/// <param name="TokenLimit">Maximum tokens this agent is allowed to consume in a single response; <c>0</c> = unbounded.</param>
/// <param name="Options">Free-form provider/model JSON overrides merged on top of host defaults.</param>
public sealed record AgentModelConfig(
    [property: JsonRequired] CompositeIdentity Id,
    ThinkLevel Think = ThinkLevel.None,
    int? ContextLength = null,
    TimeSpan? KeepAlive = null,
    int TokenLimit = 0,
    JsonElement? Options = null);
