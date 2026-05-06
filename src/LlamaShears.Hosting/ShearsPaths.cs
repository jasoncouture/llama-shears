using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace LlamaShears.Hosting;

public sealed class ShearsPaths : IShearsPaths
{
    private const string DefaultDataRootName = ".llama-shears";
    private const string DefaultWorkspaceFolderName = "workspace";
    private const string DefaultAgentsFolderName = "agents";
    private const string DefaultTemplatesFolderName = "templates";

    private readonly ConcurrentDictionary<PathKind, string> _roots;

    public ShearsPaths(IOptions<ShearsPathsOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var values = options.Value;

        var dataRoot = ResolveAndCreate(values.DataRoot, DefaultDataRoot);
        _roots = new ConcurrentDictionary<PathKind, string>
        {
            [PathKind.Data] = dataRoot,
            [PathKind.Workspace] = ResolveAndCreate(values.WorkspaceRoot, () => Path.Combine(dataRoot, DefaultWorkspaceFolderName)),
            [PathKind.Agents] = ResolveAndCreate(values.AgentsRoot, () => Path.Combine(dataRoot, DefaultAgentsFolderName)),
            [PathKind.Templates] = ResolveAndCreate(values.TemplatesRoot, () => Path.Combine(dataRoot, DefaultTemplatesFolderName)),
        };
    }

    public string DataRoot => _roots[PathKind.Data];

    public string WorkspaceRoot => _roots[PathKind.Workspace];

    public string AgentsRoot => _roots[PathKind.Agents];

    public string TemplatesRoot => _roots[PathKind.Templates];

    public string GetPath(PathKind kind, string? subpath = null)
    {
        if (!_roots.TryGetValue(kind, out var root))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown path kind.");
        }

        return string.IsNullOrWhiteSpace(subpath) ? root : Path.Combine(root, subpath);
    }

    public string GetAgentWorkspaceDefaultPath(string agentName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentName);
        return GetPath(PathKind.Workspace, agentName);
    }

    private static string DefaultDataRoot() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            DefaultDataRootName);

    private static string ResolveAndCreate(string? configured, Func<string> defaultPathFactory)
    {
        var path = string.IsNullOrWhiteSpace(configured) ? defaultPathFactory() : configured;
        return Directory.CreateDirectory(path).FullName;
    }
}
