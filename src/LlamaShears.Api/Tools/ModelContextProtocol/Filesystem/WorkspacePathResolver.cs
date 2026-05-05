using System.Globalization;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Filesystem;

internal static class WorkspacePathResolver
{
    public const string ProtectedSubfolder = "system";

    public static WorkspacePathResolution ResolveForWrite(AgentWorkspace workspace, string requestedPath)
    {
        if (string.IsNullOrWhiteSpace(requestedPath))
        {
            return WorkspacePathResolution.Failure("path is required.");
        }

        var combined = Path.IsPathRooted(requestedPath)
            ? requestedPath
            : Path.Combine(workspace.Root, requestedPath);
        var full = Path.GetFullPath(combined);

        var relative = Path.GetRelativePath(workspace.Root, full);
        if (relative.Equals("..", StringComparison.Ordinal)
            || relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            || Path.IsPathRooted(relative))
        {
            return WorkspacePathResolution.Failure(string.Format(
                CultureInfo.InvariantCulture,
                "Refused: '{0}' resolves outside the agent workspace; writes are confined to the workspace.",
                requestedPath));
        }

        var firstSegment = relative.Split(Path.DirectorySeparatorChar, 2)[0];
        if (firstSegment.Equals(ProtectedSubfolder, StringComparison.OrdinalIgnoreCase))
        {
            return WorkspacePathResolution.Failure(string.Format(
                CultureInfo.InvariantCulture,
                "Refused: '{0}' is inside the protected '{1}/' subfolder; writes there are not permitted.",
                requestedPath,
                ProtectedSubfolder));
        }

        return WorkspacePathResolution.Success(full);
    }

    public static WorkspacePathResolution ResolveWithinWorkspace(AgentWorkspace workspace, string requestedPath)
    {
        if (string.IsNullOrWhiteSpace(requestedPath))
        {
            return WorkspacePathResolution.Success(workspace.Root);
        }

        var combined = Path.IsPathRooted(requestedPath)
            ? requestedPath
            : Path.Combine(workspace.Root, requestedPath);
        var full = Path.GetFullPath(combined);

        var relative = Path.GetRelativePath(workspace.Root, full);
        if (relative.Equals("..", StringComparison.Ordinal)
            || relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            || Path.IsPathRooted(relative))
        {
            return WorkspacePathResolution.Failure(string.Format(
                CultureInfo.InvariantCulture,
                "Refused: '{0}' resolves outside the agent workspace.",
                requestedPath));
        }

        return WorkspacePathResolution.Success(full);
    }
}
