using System.Collections.Immutable;
using LlamaShears.Core.Tools.ModelContextProtocol;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Filesystem;

public sealed record FileListResult(
    string Path,
    bool Recursive,
    ImmutableArray<FileListEntry> Entries,
    int EntryCount,
    bool Truncated,
    int Cap,
    string? Error = null) : IToolResponse;
