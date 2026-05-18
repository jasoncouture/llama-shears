using System.Collections.Immutable;
using LlamaShears.Core.Tools.ModelContextProtocol;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Memory;

public sealed record MemorySearchResultPayload(
    string Query,
    double MinScore,
    int Limit,
    int HitCount,
    ImmutableArray<MemorySearchHit> Hits,
    string? Error = null) : IToolResponse;
