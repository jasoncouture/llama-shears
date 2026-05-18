namespace LlamaShears.Api.Tools.ModelContextProtocol.Filesystem;

public sealed record FileListEntry(
    string Name,
    bool IsDirectory,
    long? SizeBytes);
