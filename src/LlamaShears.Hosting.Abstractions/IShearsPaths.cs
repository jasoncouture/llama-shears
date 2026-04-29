namespace LlamaShears.Hosting;

public interface IShearsPaths
{
    string GetPath(PathKind kind, string? subpath = null);
}
