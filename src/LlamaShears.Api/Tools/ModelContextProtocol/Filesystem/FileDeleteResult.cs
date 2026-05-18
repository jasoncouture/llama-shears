using LlamaShears.Core.Tools.ModelContextProtocol;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Filesystem;

public sealed record FileDeleteResult(
    string Path,
    bool Deleted,
    bool WasDirectory,
    string? Error = null) : IToolResponse;
