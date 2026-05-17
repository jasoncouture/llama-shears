using System.Collections.Immutable;
using System.Text.Json;

namespace LlamaShears.Core.Abstractions.Provider;

/// <summary>
/// Describes one callable tool: its name, what it does (for the
/// model), and its parameter schema.
/// </summary>
/// <param name="Name">Tool name; combined with the owning <see cref="ToolGroup.Source"/> to dispatch a call.</param>
/// <param name="Description">Human-readable description shown to the model so it knows when to invoke the tool.</param>
/// <param name="Parameters">Parsed parameter list in declaration order; lossy projection of <paramref name="Schema"/> kept for caller-side introspection (required-ness, top-level type names).</param>
/// <param name="Schema">Raw JSON Schema for the tool's parameters as the source advertised it. Providers should forward this verbatim instead of rebuilding from <paramref name="Parameters"/>, since rebuilding drops fields strict validators (Gemini, structured-output) require. <see cref="JsonValueKind.Undefined"/> means the source advertised no schema.</param>
public sealed record ToolDescriptor(
    string Name,
    string Description,
    ImmutableArray<ToolParameter> Parameters,
    JsonElement Schema = default);
