using LlamaShears.Core.Abstractions.Paths;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Filesystem;

internal static class ProtectionRefusal
{
    public static string Format(string requestedPath, ProtectionMode mode, ProtectedFile rule)
    {
        var modeName = mode switch
        {
            ProtectionMode.Delete => "delete",
            ProtectionMode.Write => "write",
            ProtectionMode.Read => "read",
            ProtectionMode.Execute => "execute",
            _ => "modify",
        };
        var reason = string.IsNullOrWhiteSpace(rule.Reason) ? rule.Glob : rule.Reason;
        return $"Refused: '{requestedPath}' is protected from {modeName} ({reason}).";
    }
}
