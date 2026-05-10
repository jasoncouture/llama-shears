using System.Diagnostics.CodeAnalysis;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Filesystem;

public sealed record WorkspacePathResolution(string? FullPath, string? Error)
{
    [MemberNotNullWhen(true, nameof(FullPath))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => Error is null;

    public static WorkspacePathResolution Success(string fullPath) => new WorkspacePathResolution(fullPath, null);

    public static WorkspacePathResolution Failure(string error) => new WorkspacePathResolution(null, error);
}
