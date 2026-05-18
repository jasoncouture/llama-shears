using System.Collections.Immutable;
using LlamaShears.Core.Tools.ModelContextProtocol;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Filesystem;

public sealed record GrepResult(
    string PathGlob,
    int FilesScanned,
    int MatchCount,
    bool Truncated,
    int Cap,
    ImmutableArray<GrepMatch> Matches,
    string? Error = null) : IToolResponse;
