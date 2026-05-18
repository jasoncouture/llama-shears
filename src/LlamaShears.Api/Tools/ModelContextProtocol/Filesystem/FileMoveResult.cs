using LlamaShears.Core.Tools.ModelContextProtocol;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Filesystem;

public sealed record FileMoveResult(
    string Source,
    string Target,
    bool Moved,
    bool Overwritten,
    string? Error = null) : IToolResponse;
