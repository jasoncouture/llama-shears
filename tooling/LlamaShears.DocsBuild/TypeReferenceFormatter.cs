using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LlamaShears.DocsBuild;

internal sealed class TypeReferenceFormatter
{
    private static readonly Dictionary<string, string> Aliases = new(StringComparer.Ordinal)
    {
        { "System.Boolean", "bool" },
        { "System.Byte", "byte" },
        { "System.SByte", "sbyte" },
        { "System.Char", "char" },
        { "System.Decimal", "decimal" },
        { "System.Double", "double" },
        { "System.Single", "float" },
        { "System.Int16", "short" },
        { "System.UInt16", "ushort" },
        { "System.Int32", "int" },
        { "System.UInt32", "uint" },
        { "System.Int64", "long" },
        { "System.UInt64", "ulong" },
        { "System.Object", "object" },
        { "System.String", "string" },
        { "System.Void", "void" },
    };

    private readonly AccessibilityFilter _filter;
    private readonly string _currentRelativePath;

    public TypeReferenceFormatter(AccessibilityFilter filter, string currentRelativePath)
    {
        _filter = filter;
        _currentRelativePath = currentRelativePath;
    }

    public string FormatParameterList(string rawSignatureWithParens)
    {
        var types = XmlDocSignatureParser.ParseParameterList(rawSignatureWithParens);
        if (types.Count == 0)
        {
            return "()";
        }
        var output = new StringBuilder();
        output.Append('(');
        for (var index = 0; index < types.Count; index++)
        {
            if (index > 0)
            {
                output.Append(", ");
            }
            output.Append(Format(types[index]));
        }
        output.Append(')');
        return output.ToString();
    }

    public string FormatCref(string cref)
    {
        var prefix = cref.Length >= 2 && cref[1] == ':' ? cref[0] : '\0';
        var body = prefix == '\0' ? cref : cref.Substring(2);

        if (prefix == 'T')
        {
            var typeRef = XmlDocSignatureParser.ParseSingle(body);
            return Format(typeRef);
        }

        var sigStart = body.IndexOf('(');
        var withoutSig = sigStart >= 0 ? body.Substring(0, sigStart) : body;
        var lastDot = withoutSig.LastIndexOf('.');
        if (lastDot < 0)
        {
            return $"`{Stylize(withoutSig)}`";
        }

        var ownerFqn = withoutSig.Substring(0, lastDot);
        var memberName = withoutSig.Substring(lastDot + 1);
        var renderedOwner = FormatTypeNameWithLink(ownerFqn);
        var displayMember = memberName == "#ctor" || memberName == ".ctor"
            ? GetSimpleNameWithoutArity(GetLastSegment(ownerFqn))
            : memberName;

        return $"{renderedOwner}.`{displayMember}`";
    }

    public string Format(TypeRef typeRef)
    {
        var output = new StringBuilder();
        AppendTypeHead(output, typeRef);
        if (typeRef.GenericArgs.Count > 0)
        {
            output.Append('<');
            for (var index = 0; index < typeRef.GenericArgs.Count; index++)
            {
                if (index > 0)
                {
                    output.Append(", ");
                }
                output.Append(Format(typeRef.GenericArgs[index]));
            }
            output.Append('>');
        }
        for (var rank = 0; rank < typeRef.ArrayRank; rank++)
        {
            output.Append("[]");
        }
        if (typeRef.IsByRef)
        {
            output.Append('&');
        }
        if (typeRef.IsPointer)
        {
            output.Append('*');
        }
        return output.ToString();
    }

    private void AppendTypeHead(StringBuilder output, TypeRef typeRef)
    {
        var fullName = typeRef.FullName;
        if (Aliases.TryGetValue(fullName, out var alias))
        {
            output.Append(alias);
            return;
        }
        if (fullName.StartsWith("`", StringComparison.Ordinal))
        {
            output.Append(Stylize(fullName));
            return;
        }
        output.Append(FormatTypeNameWithLink(fullName));
    }

    private string FormatTypeNameWithLink(string fullName)
    {
        var simpleName = GetSimpleNameWithoutArity(GetLastSegment(fullName));
        var targetRelative = _filter.GetMarkdownRelativePath(fullName);
        if (targetRelative is null)
        {
            return simpleName;
        }
        var link = ComputeRelativeLink(_currentRelativePath, targetRelative);
        return $"[{simpleName}]({link})";
    }

    private static string GetLastSegment(string fullName)
    {
        var lastDot = fullName.LastIndexOf('.');
        return lastDot < 0 ? fullName : fullName.Substring(lastDot + 1);
    }

    private static string GetSimpleNameWithoutArity(string name)
    {
        var backtick = name.IndexOf('`');
        return backtick < 0 ? name : name.Substring(0, backtick);
    }

    private static string Stylize(string raw)
    {
        return raw.Replace('`', '_');
    }

    private static string ComputeRelativeLink(string fromRelative, string toRelative)
    {
        var fromParts = fromRelative.Replace(Path.DirectorySeparatorChar, '/').Split('/');
        var toParts = toRelative.Replace(Path.DirectorySeparatorChar, '/').Split('/');

        var commonPrefix = 0;
        while (commonPrefix < fromParts.Length - 1
            && commonPrefix < toParts.Length - 1
            && string.Equals(fromParts[commonPrefix], toParts[commonPrefix], StringComparison.Ordinal))
        {
            commonPrefix++;
        }

        var ups = (fromParts.Length - 1) - commonPrefix;
        var rest = toParts.Skip(commonPrefix);
        var pieces = Enumerable.Repeat("..", ups).Concat(rest);
        return string.Join("/", pieces);
    }
}
