using System.Collections.Immutable;

namespace LlamaShears.Core.Abstractions.Context;

public sealed record PluginContext(ImmutableDictionary<string, object> Data);
