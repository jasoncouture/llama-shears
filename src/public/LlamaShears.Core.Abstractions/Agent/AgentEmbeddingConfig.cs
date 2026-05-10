using System.Text.Json;
using System.Text.Json.Serialization;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Core.Abstractions.Agent;

/// <summary>
/// Per-agent embedding-model selection used for memory search.
/// Asymmetric prefixes are supplied here so the framework, not the
/// caller, knows whether to decorate "this is a query" vs "this is a
/// document being indexed".
/// </summary>
/// <param name="Id">Provider/model identifier of the embedding model.</param>
/// <param name="KeepAlive">Provider-specific keep-alive for the model; <see langword="null"/> uses provider default.</param>
/// <param name="QueryPrefix">Prefix prepended to texts being embedded as a query (asymmetric models only).</param>
/// <param name="DocumentPrefix">Prefix prepended to texts being embedded as a document (asymmetric models only).</param>
/// <param name="Options">Free-form provider/model JSON overrides merged on top of host defaults.</param>
public sealed record AgentEmbeddingConfig(
    [property: JsonRequired] ModelIdentity Id,
    TimeSpan? KeepAlive = null,
    string? QueryPrefix = null,
    string? DocumentPrefix = null,
    JsonElement? Options = null);
