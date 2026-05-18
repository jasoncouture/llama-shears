using LlamaShears.Core.Tools.ModelContextProtocol;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Filesystem;

public sealed record FileReadResult(
    string Path,
    int StartLine,
    int EndLine,
    int LinesReturned,
    bool EndOfFile,
    int? NextStartLine,
    string Content,
    string? Error = null) : IToolResponse;
