using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace LlamaShears.DocsBuild;

internal sealed class MarkdownRenderer
{
    private readonly TypeReferenceFormatter _formatter;
    private readonly string _typeFqn;

    private MarkdownRenderer(TypeReferenceFormatter formatter, string typeFqn)
    {
        _formatter = formatter;
        _typeFqn = typeFqn;
    }

    public static string Render(
        string typeFqn,
        IReadOnlyList<MemberDoc> members,
        string assemblyName,
        AccessibilityFilter filter,
        string currentRelativePath)
    {
        var formatter = new TypeReferenceFormatter(filter, currentRelativePath);
        var renderer = new MarkdownRenderer(formatter, typeFqn);
        return renderer.RenderInternal(members, assemblyName);
    }

    private string RenderInternal(IReadOnlyList<MemberDoc> members, string assemblyName)
    {
        var output = new StringBuilder();
        var typeMember = members.FirstOrDefault(static m => m.Kind == MemberKind.Type);

        output.Append("# ").AppendLine(_typeFqn);
        output.AppendLine();
        output.Append("Assembly: `").Append(assemblyName).AppendLine("`");
        output.AppendLine();

        if (typeMember is not null)
        {
            RenderMemberBody(typeMember, output, headingLevel: 0);
        }

        RenderSection(output, members, MemberKind.Field, "Fields");
        RenderSection(output, members, MemberKind.Property, "Properties");
        RenderSection(output, members, MemberKind.Method, "Methods");
        RenderSection(output, members, MemberKind.Event, "Events");

        return output.ToString();
    }

    private void RenderSection(
        StringBuilder output,
        IReadOnlyList<MemberDoc> members,
        MemberKind kind,
        string heading)
    {
        var bucket = members
            .Where(m => m.Kind == kind)
            .OrderBy(static m => m.MemberName, StringComparer.Ordinal)
            .ThenBy(static m => m.ParameterSignature, StringComparer.Ordinal)
            .ToList();
        if (bucket.Count == 0)
        {
            return;
        }

        output.Append("## ").AppendLine(heading);
        output.AppendLine();

        foreach (var member in bucket)
        {
            output.Append("### ").AppendLine(FormatMemberSignature(member));
            output.AppendLine();
            RenderMemberBody(member, output, headingLevel: 4);
        }
    }

    private string FormatMemberSignature(MemberDoc member)
    {
        var displayName = member.MemberName;
        if (displayName == "#ctor" || displayName == "#cctor")
        {
            displayName = GetSimpleTypeName(_typeFqn);
        }

        if (member.ParameterSignature is null)
        {
            return $"`{displayName}`";
        }

        var parameterList = _formatter.FormatParameterList(member.ParameterSignature);
        return $"`{displayName}`{parameterList}";
    }

    private static string GetSimpleTypeName(string fqn)
    {
        var lastDot = fqn.LastIndexOf('.');
        var name = lastDot < 0 ? fqn : fqn.Substring(lastDot + 1);
        var backtick = name.IndexOf('`');
        return backtick < 0 ? name : name.Substring(0, backtick);
    }

    private void RenderMemberBody(MemberDoc member, StringBuilder output, int headingLevel)
    {
        var element = member.Element;

        var summary = element.Element("summary");
        if (summary is not null)
        {
            AppendBlock(output, summary);
        }

        var typeParams = element.Elements("typeparam").ToList();
        if (typeParams.Count > 0)
        {
            AppendList(output, headingLevel, "Type Parameters", typeParams, "name");
        }

        var parameters = element.Elements("param").ToList();
        if (parameters.Count > 0)
        {
            AppendList(output, headingLevel, "Parameters", parameters, "name");
        }

        var returns = element.Element("returns");
        if (returns is not null)
        {
            AppendNamedSection(output, headingLevel, "Returns", returns);
        }

        var value = element.Element("value");
        if (value is not null)
        {
            AppendNamedSection(output, headingLevel, "Value", value);
        }

        var remarks = element.Element("remarks");
        if (remarks is not null)
        {
            AppendNamedSection(output, headingLevel, "Remarks", remarks);
        }

        var examples = element.Elements("example").ToList();
        foreach (var example in examples)
        {
            AppendNamedSection(output, headingLevel, "Example", example);
        }

        var exceptions = element.Elements("exception").ToList();
        if (exceptions.Count > 0)
        {
            AppendList(output, headingLevel, "Exceptions", exceptions, "cref");
        }

        var seeAlso = element.Elements("seealso").ToList();
        if (seeAlso.Count > 0)
        {
            output.AppendLine(headingLevel == 0 ? "## See Also" : "#### See Also");
            output.AppendLine();
            foreach (var item in seeAlso)
            {
                var cref = (string?)item.Attribute("cref");
                var href = (string?)item.Attribute("href");
                if (cref is not null)
                {
                    output.Append("- ").AppendLine(_formatter.FormatCref(cref));
                }
                else if (href is not null)
                {
                    output.Append("- [").Append(href).Append("](").Append(href).AppendLine(")");
                }
            }
            output.AppendLine();
        }
    }

    private void AppendBlock(StringBuilder output, XElement element)
    {
        var rendered = RenderInline(element).Trim();
        if (rendered.Length == 0)
        {
            return;
        }
        output.AppendLine(rendered);
        output.AppendLine();
    }

    private void AppendNamedSection(StringBuilder output, int headingLevel, string heading, XElement element)
    {
        var prefix = headingLevel == 0 ? "## " : "#### ";
        output.Append(prefix).AppendLine(heading);
        output.AppendLine();
        AppendBlock(output, element);
    }

    private void AppendList(
        StringBuilder output,
        int headingLevel,
        string heading,
        IReadOnlyList<XElement> items,
        string keyAttribute)
    {
        var prefix = headingLevel == 0 ? "## " : "#### ";
        output.Append(prefix).AppendLine(heading);
        output.AppendLine();
        foreach (var item in items)
        {
            var rawKey = (string?)item.Attribute(keyAttribute) ?? "";
            string displayKey;
            if (keyAttribute == "cref")
            {
                displayKey = _formatter.FormatCref(rawKey);
            }
            else
            {
                displayKey = $"`{rawKey}`";
            }
            var body = RenderInline(item).Trim();
            if (body.Length == 0)
            {
                output.Append("- ").AppendLine(displayKey);
            }
            else
            {
                output.Append("- ").Append(displayKey).Append(" — ").AppendLine(body);
            }
        }
        output.AppendLine();
    }

    private string RenderInline(XElement element)
    {
        var output = new StringBuilder();
        RenderInlineNodes(element.Nodes(), output);
        return CollapseWhitespace(output.ToString());
    }

    private void RenderInlineNodes(IEnumerable<XNode> nodes, StringBuilder output)
    {
        foreach (var node in nodes)
        {
            switch (node)
            {
                case XText text:
                    output.Append(text.Value);
                    break;
                case XElement child:
                    RenderInlineElement(child, output);
                    break;
            }
        }
    }

    private void RenderInlineElement(XElement element, StringBuilder output)
    {
        switch (element.Name.LocalName)
        {
            case "see":
            case "seealso":
                {
                    var cref = (string?)element.Attribute("cref");
                    var href = (string?)element.Attribute("href");
                    var langword = (string?)element.Attribute("langword");
                    if (cref is not null)
                    {
                        output.Append(_formatter.FormatCref(cref));
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
            case "code":
                output.AppendLine();
                output.AppendLine();
                output.AppendLine("```");
                output.AppendLine(TrimCode(element.Value));
                output.AppendLine("```");
                output.AppendLine();
                break;
            case "para":
                output.AppendLine();
                output.AppendLine();
                RenderInlineNodes(element.Nodes(), output);
                output.AppendLine();
                output.AppendLine();
                break;
            case "list":
                RenderList(element, output);
                break;
            default:
                RenderInlineNodes(element.Nodes(), output);
                break;
        }
    }

    private void RenderList(XElement listElement, StringBuilder output)
    {
        output.AppendLine();
        var items = listElement.Elements("item").ToList();
        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            var description = item.Element("description") ?? item;
            var rendered = RenderInline(description).Trim();
            output.Append("- ").AppendLine(rendered);
        }
        output.AppendLine();
    }

    private static string CollapseWhitespace(string raw)
    {
        var lines = raw.Replace("\r\n", "\n").Split('\n');
        var output = new StringBuilder();
        foreach (var line in lines)
        {
            output.AppendLine(line.Trim());
        }
        return output.ToString().Trim();
    }

    private static string TrimCode(string raw)
    {
        var lines = raw.Replace("\r\n", "\n").Split('\n');
        var trimmed = lines
            .SkipWhile(static line => string.IsNullOrWhiteSpace(line))
            .Reverse()
            .SkipWhile(static line => string.IsNullOrWhiteSpace(line))
            .Reverse()
            .ToList();
        if (trimmed.Count == 0)
        {
            return "";
        }
        var commonIndent = trimmed
            .Where(static line => !string.IsNullOrWhiteSpace(line))
            .Select(static line => line.Length - line.TrimStart(' ').Length)
            .DefaultIfEmpty(0)
            .Min();
        return string.Join("\n", trimmed.Select(line =>
            line.Length >= commonIndent ? line.Substring(commonIndent) : line));
    }
}
