using System.Globalization;
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
        return string.Format(
            CultureInfo.InvariantCulture,
            "Refused: '{0}' is protected from {1} ({2}).",
            requestedPath,
            modeName,
            reason);
    }
}
