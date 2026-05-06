using System.Linq;

namespace LlamaShears.DocsBuild;

internal static class TypePathLayout
{
    public static string GetMarkdownRelativePath(string typeFqn)
    {
        var lastDot = typeFqn.LastIndexOf('.');
        string namespacePart;
        string typeName;
        if (lastDot < 0)
        {
            namespacePart = "";
            typeName = typeFqn;
        }
        else
        {
            namespacePart = typeFqn.Substring(0, lastDot);
            typeName = typeFqn.Substring(lastDot + 1);
        }
        var fileBase = GetFileBaseName(typeName);
        return namespacePart.Length == 0
            ? $"{fileBase}.md"
            : $"{namespacePart.Replace('.', '/')}/{fileBase}.md";
    }

    public static string GetFileBaseName(string typeName)
    {
        var stripped = StripArity(typeName);
        var arity = GetArity(typeName);
        return arity > 0 ? $"{stripped}-{arity}" : stripped;
    }

    public static string StripArity(string typeName)
    {
        var backtick = typeName.IndexOf('`');
        return backtick < 0 ? typeName : typeName.Substring(0, backtick);
    }

    public static int GetArity(string typeName)
    {
        var backtick = typeName.IndexOf('`');
        if (backtick < 0)
        {
            return 0;
        }
        return int.TryParse(typeName.Substring(backtick + 1), out var arity) ? arity : 0;
    }

    public static string FormatTypeDisplay(string typeName)
    {
        var backtick = typeName.IndexOf('`');
        if (backtick < 0)
        {
            return typeName;
        }
        var simple = typeName.Substring(0, backtick);
        if (!int.TryParse(typeName.Substring(backtick + 1), out var arity) || arity <= 0)
        {
            return simple;
        }
        if (arity == 1)
        {
            return $"{simple}<T>";
        }
        var parts = string.Join(", ", Enumerable.Range(1, arity).Select(i => $"T{i}"));
        return $"{simple}<{parts}>";
    }

    public static string FormatFqnDisplay(string typeFqn)
    {
        var lastDot = typeFqn.LastIndexOf('.');
        if (lastDot < 0)
        {
            return FormatTypeDisplay(typeFqn);
        }
        var namespacePart = typeFqn.Substring(0, lastDot);
        var typeName = typeFqn.Substring(lastDot + 1);
        return $"{namespacePart}.{FormatTypeDisplay(typeName)}";
    }
}
