using System.Text.Json.Serialization;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Core.Abstractions.Agent;

public sealed record AgentModelConfig(
    [property: JsonRequired] ModelIdentity Id,
    ThinkLevel Think = ThinkLevel.None,
    int? ContextLength = null,
    TimeSpan? KeepAlive = null,
    int TokenLimit = 0);
