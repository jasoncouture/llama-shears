using System.Collections.Immutable;

namespace LlamaShears.Core.Abstractions.Context;

/// <summary>
/// A bundle of <see cref="ToolDescriptor"/>s sharing a single
/// <see cref="Source"/> (e.g. an MCP server slug). Used as the
/// prompt-time grouping; the framework dispatches tool calls by
/// pairing <see cref="Source"/> with the model-supplied tool name.
/// </summary>
/// <param name="Source">Logical owner / dispatch key for the tools in this group.</param>
/// <param name="Tools">Tools owned by this source.</param>
public sealed record ToolGroup(
    string Source,
    ImmutableArray<ToolDescriptor> Tools);
