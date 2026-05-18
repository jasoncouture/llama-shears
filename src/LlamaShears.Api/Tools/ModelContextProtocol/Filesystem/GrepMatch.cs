namespace LlamaShears.Api.Tools.ModelContextProtocol.Filesystem;

public sealed record GrepMatch(
    string Path,
    int Line,
    int Column,
    string Text);
