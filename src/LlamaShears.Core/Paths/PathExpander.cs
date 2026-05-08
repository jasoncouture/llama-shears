using LlamaShears.Core.Abstractions.Paths;

namespace LlamaShears.Core.Paths;

public sealed class PathExpander : IPathExpander
{
    private const string HomePrefix = "~/";

    public string ExpandPath(string path, string workingDirectory)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentException.ThrowIfNullOrEmpty(workingDirectory);

        if (Path.IsPathRooted(path))
        {
            return path;
        }

        if (path == "~")
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        if (path.StartsWith(HomePrefix, StringComparison.Ordinal))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, path[HomePrefix.Length..]);
        }

        return Path.GetFullPath(Path.Combine(workingDirectory, path));
    }
}
