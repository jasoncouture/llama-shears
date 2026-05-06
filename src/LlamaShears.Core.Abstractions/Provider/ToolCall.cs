namespace LlamaShears.Core.Abstractions.Provider;

/// <summary>
/// One tool the model is asking the host to invoke. The host pairs
/// <see cref="Source"/> + <see cref="Name"/> against the registered
/// tool catalog to find the right handler.
/// </summary>
/// <param name="Source">Logical group the tool belongs to (e.g. an MCP server slug or framework prefix).</param>
/// <param name="Name">Tool name within <paramref name="Source"/>.</param>
/// <param name="ArgumentsJson">Tool arguments serialized as JSON exactly as the model produced them.</param>
/// <param name="CallId">Provider-supplied correlation id; used to pair a call with its result. <see langword="null"/> when the provider does not surface one.</param>
public sealed record ToolCall(
    string Source,
    string Name,
    string ArgumentsJson,
    string? CallId = null);
