using LlamaShears.Core.Tools.ModelContextProtocol;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Memory;

public sealed record MemoryStoreResult(
    bool Stored,
    string? RelativePath,
    string? Error = null) : IToolResponse;
