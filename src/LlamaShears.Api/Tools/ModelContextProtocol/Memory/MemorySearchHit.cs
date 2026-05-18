namespace LlamaShears.Api.Tools.ModelContextProtocol.Memory;

public sealed record MemorySearchHit(
    string RelativePath,
    double Score,
    string Summary);
