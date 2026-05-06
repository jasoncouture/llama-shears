using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LlamaShears.DocsBuild;

internal static class AssemblyIndexRenderer
{
    public static string Render(string assemblyName, IEnumerable<string> typeFqns, string? readmeContent = null)
    {
        var grouped = typeFqns
            .Select(fqn => (Fqn: fqn, NamespaceName: SplitNamespace(fqn), TypeName: SplitTypeName(fqn)))
            .GroupBy(static entry => entry.NamespaceName, StringComparer.Ordinal)
            .OrderBy(static group => group.Key, StringComparer.Ordinal);

        var output = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(readmeContent))
        {
            output.AppendLine(readmeContent!.TrimEnd());
            output.AppendLine();
            output.AppendLine("---");
            output.AppendLine();
        }
        else
        {
            output.Append("# ").AppendLine(assemblyName);
            output.AppendLine();
            output.AppendLine("Public API surface, organized by namespace.");
            output.AppendLine();
        }

        foreach (var group in grouped)
        {
            var namespaceLabel = string.IsNullOrEmpty(group.Key) ? "(global namespace)" : group.Key;
            output.Append("## ").AppendLine(namespaceLabel);
            output.AppendLine();

            foreach (var entry in group.OrderBy(static t => t.TypeName, StringComparer.Ordinal))
            {
                var displayName = StripArity(entry.TypeName);
                var relativePath = ToRelativeMarkdownLink(entry.Fqn);
                output.Append("- [").Append(displayName).Append("](").Append(relativePath).AppendLine(")");
            }
            output.AppendLine();
        }

        return output.ToString();
    }

    private static string SplitNamespace(string fqn)
    {
        var lastDot = fqn.LastIndexOf('.');
        return lastDot < 0 ? "" : fqn.Substring(0, lastDot);
    }

    private static string SplitTypeName(string fqn)
    {
        var lastDot = fqn.LastIndexOf('.');
        return lastDot < 0 ? fqn : fqn.Substring(lastDot + 1);
    }

    private static string StripArity(string typeName)
    {
        var backtick = typeName.IndexOf('`');
        return backtick < 0 ? typeName : typeName.Substring(0, backtick);
    }

    private static string ToRelativeMarkdownLink(string fqn)
    {
        var lastDot = fqn.LastIndexOf('.');
        if (lastDot < 0)
        {
            return $"{fqn}.md";
        }
        var folders = fqn.Substring(0, lastDot).Replace('.', '/');
        var typeName = fqn.Substring(lastDot + 1);
        return $"{folders}/{typeName}.md";
    }
}
