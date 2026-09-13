using LlamaShears.Core.Tools.ModelContextProtocol;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Compaction;

public sealed record ContextCompactResult(
    bool Compacted,
    int PreservedTurns = 0,
    string? Error = null) : IToolResponse;
