namespace LlamaShears.Core.Abstractions.Events.Agent;

/// <summary>
/// Event-bus payload describing a single tool call the agent is about
/// to dispatch. Mirrors the provider-layer ToolCall record; kept
/// distinct so consumers of the event bus don't have to depend on the
/// provider layer.
/// </summary>
/// <param name="Source">Logical owner of the tool (e.g. an MCP server slug).</param>
/// <param name="Name">Tool name within <paramref name="Source"/>.</param>
/// <param name="ArgumentsJson">Tool arguments serialized as JSON exactly as the model produced them.</param>
/// <param name="CallId">Provider-supplied correlation id; <see langword="null"/> when the provider does not surface one.</param>
public sealed record AgentToolCallFragment(
    string Source,
    string Name,
    string ArgumentsJson,
    string? CallId = null);
