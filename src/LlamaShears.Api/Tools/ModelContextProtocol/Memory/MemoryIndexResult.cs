using LlamaShears.Core.Tools.ModelContextProtocol;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Memory;

public sealed record MemoryIndexResult(
    bool Reconciled,
    int Added,
    int Updated,
    int Removed,
    int Total,
    double ElapsedMilliseconds,
    string? Error = null) : IToolResponse;
