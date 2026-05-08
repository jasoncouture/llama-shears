using LlamaShears.Core.Abstractions.Paths;
using LlamaShears.Core.Abstractions.SystemPrompt;

namespace LlamaShears.Core.SystemPrompt;

public sealed class TemplateFileLocator : ITemplateFileLocator
{
    private const string WorkspaceSystemSubpath = "system";
    private const string TemplatesWorkspaceSystemSubpath = "workspace/system";
    private const string BundledSubpath = "content/templates/workspace/system";

    private readonly IShearsPaths _paths;
    private readonly string _bundledRoot;

    public TemplateFileLocator(IShearsPaths paths)
    {
        _paths = paths;
        _bundledRoot = Path.Combine(AppContext.BaseDirectory, BundledSubpath);
    }

    public string? Locate(string? subFolder, string fileName, string defaultFileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultFileName);

        var workspaceRoot = _paths.GetPath(PathKind.Workspace, WorkspaceSystemSubpath);
        var templatesRoot = _paths.GetPath(PathKind.Templates, TemplatesWorkspaceSystemSubpath);

        var roots = new[] { workspaceRoot, templatesRoot, _bundledRoot };
        var sameName = string.Equals(fileName, defaultFileName, StringComparison.Ordinal);

        foreach (var root in roots)
        {
            var dir = string.IsNullOrEmpty(subFolder) ? root : Path.Combine(root, subFolder);
            var primary = Path.Combine(dir, fileName);
            if (File.Exists(primary))
            {
                return primary;
            }
            if (sameName)
            {
                continue;
            }
            var fallback = Path.Combine(dir, defaultFileName);
            if (File.Exists(fallback))
            {
                return fallback;
            }
        }
        return null;
    }
}
