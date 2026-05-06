using System.Text.Json.Serialization;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Core.Abstractions.Agent;

public sealed record AgentEmbeddingConfig(
    [property: JsonRequired] ModelIdentity Id,
    TimeSpan? KeepAlive = null);
