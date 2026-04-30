using System.Collections.Immutable;

namespace LlamaShears.Core.Abstractions.Context;

/// <summary>
/// Template-shaped view of a single tool. The fields are intentionally
/// flat and string-typed so Scriban can render them without any
/// schema-aware helper. Richer metadata (full JSON schema, handler
/// reference, security policy) belongs alongside the tool implementation
/// itself, not on this descriptor.
/// </summary>
/// <param name="Name">Identifier the model calls the tool by.</param>
/// <param name="Description">Human-readable description shown to the model.</param>
/// <param name="Parameters">Ordered list of accepted inputs.</param>
public sealed record ToolDescriptor(
    string Name,
    string Description,
    ImmutableArray<ToolParameter> Parameters);
