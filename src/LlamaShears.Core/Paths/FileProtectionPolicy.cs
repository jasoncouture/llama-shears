using System.Collections.Immutable;
using LlamaShears.Core.Abstractions.Paths;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Options;

namespace LlamaShears.Core.Paths;

public sealed class FileProtectionPolicy : IFileProtectionPolicy
{
    private readonly ImmutableArray<CompiledRule> _rules;

    public FileProtectionPolicy(IOptions<FileProtectionPolicyOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _rules = [.. options.Value.Rules.Select(static r => new CompiledRule(r, BuildMatcher(r.Glob)))];
    }

    public ProtectedFile? Match(string workspaceRoot, string fullPath, FileType actualType, ProtectionMode requestedMode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(fullPath);
        if (requestedMode == ProtectionMode.None || actualType == FileType.None)
        {
            return null;
        }

        var (matchRoot, matchPath) = ResolveMatchRoot(workspaceRoot, fullPath);
        var directory = new InMemoryDirectoryInfo(matchRoot, [matchPath]);
        foreach (var rule in _rules)
        {
            if ((rule.Source.ProtectionMode & requestedMode) == 0)
            {
                continue;
            }
            if ((rule.Source.Type & actualType) == 0)
            {
                continue;
            }
            if (!rule.Matcher.Execute(directory).HasMatches)
            {
                continue;
            }
            return rule.Source;
        }
        return null;
    }

    private static (string Root, string Path) ResolveMatchRoot(string workspaceRoot, string fullPath)
    {
        var relative = Path.GetRelativePath(workspaceRoot, fullPath);
        if (!relative.StartsWith("..", StringComparison.Ordinal) && !Path.IsPathRooted(relative))
        {
            return (workspaceRoot, fullPath);
        }
        var systemRoot = Path.GetPathRoot(fullPath);
        if (string.IsNullOrEmpty(systemRoot))
        {
            return (workspaceRoot, fullPath);
        }
        return (systemRoot, fullPath);
    }

    private static Matcher BuildMatcher(string glob)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(glob);
        var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
        matcher.AddInclude(glob);
        return matcher;
    }

    private sealed record CompiledRule(ProtectedFile Source, Matcher Matcher);
}
