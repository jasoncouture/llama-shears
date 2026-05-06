using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace LlamaShears.DocsBuild;

internal static class NamespaceIndexRenderer
{
    public static IReadOnlyDictionary<string, string> RenderAll(
        IReadOnlyDictionary<string, List<MemberDoc>> byType,
        AccessibilityFilter filter,
        HashSet<string> writtenTypes)
    {
        var typeFqns = byType.Keys.ToList();
        var namespaces = new HashSet<string>(
            typeFqns.Select(GetNamespace).Where(static ns => !string.IsNullOrEmpty(ns)),
            StringComparer.Ordinal);

        var output = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var namespaceName in namespaces.OrderBy(static ns => ns, StringComparer.Ordinal))
        {
            var content = RenderOne(namespaceName, byType, namespaces, filter, writtenTypes);
            var relativePath = $"{namespaceName.Replace('.', '/')}/index.md";
            output[relativePath] = content;
        }
        return output;
    }

    private static string RenderOne(
        string namespaceName,
        IReadOnlyDictionary<string, List<MemberDoc>> byType,
        IReadOnlyCollection<string> allNamespaces,
        AccessibilityFilter filter,
        HashSet<string> writtenTypes)
    {
        var indexRelativePath = $"{namespaceName.Replace('.', '/')}/index.md";
        var formatter = new TypeReferenceFormatter(filter, writtenTypes, indexRelativePath);

        var output = new StringBuilder();
        output.Append("# ").AppendLine(namespaceName);
        output.AppendLine();

        var prefix = $"{namespaceName}.";
        var directChildren = allNamespaces
            .Where(ns => ns.StartsWith(prefix, StringComparison.Ordinal)
                && ns.IndexOf('.', prefix.Length) < 0)
            .OrderBy(static ns => ns, StringComparer.Ordinal)
            .ToList();

        if (directChildren.Count > 0)
        {
            output.AppendLine("## Namespaces");
            output.AppendLine();
            foreach (var child in directChildren)
            {
                var leaf = child.Substring(prefix.Length);
                output.Append("- [").Append(child).Append("](").Append(leaf).AppendLine("/index.md)");
            }
            output.AppendLine();
        }

        var typesInNamespace = byType
            .Where(pair => GetNamespace(pair.Key) == namespaceName)
            .OrderBy(static pair => GetTypeName(pair.Key), StringComparer.Ordinal)
            .ToList();

        if (typesInNamespace.Count > 0)
        {
            output.AppendLine("## Types");
            output.AppendLine();
            foreach (var pair in typesInNamespace)
            {
                var typeName = GetTypeName(pair.Key);
                var displayName = TypePathLayout.FormatTypeDisplay(typeName);
                var fileBase = TypePathLayout.GetFileBaseName(typeName);
                var typeMember = pair.Value.FirstOrDefault(static m => m.Kind == MemberKind.Type);
                var summary = typeMember is null ? "" : RenderSummary(typeMember.Element, formatter);

                output.Append("- [").Append(displayName).Append("](").Append(fileBase).Append(".md)");
                if (!string.IsNullOrEmpty(summary))
                {
                    output.Append(" — ").Append(summary);
                }
                output.AppendLine();
            }
            output.AppendLine();
        }

        return output.ToString();
    }

    private static string RenderSummary(XElement memberElement, TypeReferenceFormatter formatter)
    {
        var summary = memberElement.Element("summary");
        if (summary is null)
        {
            return "";
        }

        var output = new StringBuilder();
        AppendInline(summary.Nodes(), output, formatter);
        var raw = output.ToString();

        var paragraphBreak = raw.IndexOf("\n\n", StringComparison.Ordinal);
        if (paragraphBreak >= 0)
        {
            raw = raw.Substring(0, paragraphBreak);
        }

        return CollapseWhitespace(raw);
    }

    private static void AppendInline(IEnumerable<XNode> nodes, StringBuilder output, TypeReferenceFormatter formatter)
    {
        foreach (var node in nodes)
        {
            switch (node)
            {
                case XText text:
                    output.Append(text.Value);
                    break;
                case XElement element:
                    AppendInlineElement(element, output, formatter);
                    break;
            }
        }
    }

    private static void AppendInlineElement(XElement element, StringBuilder output, TypeReferenceFormatter formatter)
    {
        switch (element.Name.LocalName)
        {
            case "see":
            case "seealso":
                {
                    var cref = (string?)element.Attribute("cref");
                    var langword = (string?)element.Attribute("langword");
                    var href = (string?)element.Attribute("href");
                    if (cref is not null)
                    {
                        output.Append(formatter.FormatCref(cref));
                    }
                    else if (langword is not null)
                    {
                        output.Append('`').Append(langword).Append('`');
                    }
                    else if (href is not null)
                    {
                        var label = element.Value;
                        if (string.IsNullOrWhiteSpace(label))
                        {
                            label = href;
                        }
                        output.Append('[').Append(label).Append("](").Append(href).Append(')');
                    }
                    break;
                }
            case "paramref":
            case "typeparamref":
                {
                    var name = (string?)element.Attribute("name") ?? "";
                    output.Append('`').Append(name).Append('`');
                    break;
                }
            case "c":
                output.Append('`').Append(element.Value).Append('`');
                break;
            case "para":
                output.Append("\n\n");
                AppendInline(element.Nodes(), output, formatter);
                output.Append("\n\n");
                break;
            default:
                AppendInline(element.Nodes(), output, formatter);
                break;
        }
    }

    private static string CollapseWhitespace(string raw)
    {
        var output = new StringBuilder(raw.Length);
        var lastWasSpace = false;
        foreach (var ch in raw)
        {
            if (ch == '\r' || ch == '\n' || ch == '\t' || ch == ' ')
            {
                if (!lastWasSpace)
                {
                    output.Append(' ');
                    lastWasSpace = true;
                }
            }
            else
            {
                output.Append(ch);
                lastWasSpace = false;
            }
        }
        return output.ToString().Trim();
    }

    private static string GetNamespace(string typeFqn)
    {
        var lastDot = typeFqn.LastIndexOf('.');
        return lastDot < 0 ? "" : typeFqn.Substring(0, lastDot);
    }

    private static string GetTypeName(string typeFqn)
    {
        var lastDot = typeFqn.LastIndexOf('.');
        return lastDot < 0 ? typeFqn : typeFqn.Substring(lastDot + 1);
    }

}
