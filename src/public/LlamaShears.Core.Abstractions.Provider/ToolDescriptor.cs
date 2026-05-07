using System.Collections.Immutable;

namespace LlamaShears.Core.Abstractions.Provider;

/// <summary>
/// Describes one callable tool: its name, what it does (for the
/// model), and its parameter schema.
/// </summary>
/// <param name="Name">Tool name; combined with the owning <see cref="ToolGroup.Source"/> to dispatch a call.</param>
/// <param name="Description">Human-readable description shown to the model so it knows when to invoke the tool.</param>
/// <param name="Parameters">Parameter schema in declaration order.</param>
public sealed record ToolDescriptor(
    string Name,
    string Description,
    ImmutableArray<ToolParameter> Parameters);
