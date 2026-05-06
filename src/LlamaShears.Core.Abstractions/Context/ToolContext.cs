using System.Collections.Immutable;

namespace LlamaShears.Core.Abstractions.Context;

/// <summary>
/// Frozen view of the tools visible to the agent at the moment the
/// snapshot was composed. Scope (turn/agent/global, allow/deny lists,
/// plugin contributions) is resolved by the composer; this record just
/// exposes the materialized list for templates and observers to read.
/// </summary>
public sealed record ToolContext(ImmutableArray<ToolDescriptor> Items);
