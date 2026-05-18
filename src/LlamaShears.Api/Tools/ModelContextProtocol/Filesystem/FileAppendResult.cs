using LlamaShears.Core.Tools.ModelContextProtocol;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Filesystem;

public sealed record FileAppendResult(
    string Path,
    bool Appended,
    int BytesAppended,
    string? Error = null) : IToolResponse;
