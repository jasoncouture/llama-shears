namespace LlamaShears.Core.Abstractions.Paths;

public interface IShearsPaths
{
    string GetPath(PathKind kind, string? subpath = null, bool ensureExists = false);
}
