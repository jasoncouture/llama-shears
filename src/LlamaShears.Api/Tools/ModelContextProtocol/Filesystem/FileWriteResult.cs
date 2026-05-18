using LlamaShears.Core.Tools.ModelContextProtocol;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Filesystem;

public sealed record FileWriteResult(
    string Path,
    bool Written,
    int BytesWritten,
    bool Overwritten,
    string? Error = null) : IToolResponse;
