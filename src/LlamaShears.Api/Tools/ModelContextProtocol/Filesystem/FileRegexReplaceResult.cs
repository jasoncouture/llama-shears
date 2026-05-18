using LlamaShears.Core.Tools.ModelContextProtocol;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Filesystem;

public sealed record FileRegexReplaceResult(
    string Path,
    bool Edited,
    int Replacements,
    string? Error = null) : IToolResponse;
