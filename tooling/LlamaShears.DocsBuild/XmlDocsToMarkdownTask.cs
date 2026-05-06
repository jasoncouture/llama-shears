using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace LlamaShears.DocsBuild;

public sealed class XmlDocsToMarkdownTask : Task
{
    [Required]
    public string DocumentationFile { get; set; } = "";

    [Required]
    public string OutputDirectory { get; set; } = "";

    [Required]
    public string AssemblyName { get; set; } = "";

    [Required]
    public string AssemblyPath { get; set; } = "";

    public override bool Execute()
    {
        if (!File.Exists(DocumentationFile))
        {
            Log.LogError("XmlDocsToMarkdownTask: DocumentationFile '{0}' does not exist.", DocumentationFile);
            return false;
        }

        if (!File.Exists(AssemblyPath))
        {
            Log.LogError("XmlDocsToMarkdownTask: AssemblyPath '{0}' does not exist.", AssemblyPath);
            return false;
        }

        XDocument xml;
        try
        {
            xml = XDocument.Load(DocumentationFile);
        }
        catch (Exception ex)
        {
            Log.LogError("XmlDocsToMarkdownTask: failed to parse '{0}': {1}", DocumentationFile, ex.Message);
            return false;
        }

        AccessibilityFilter filter;
        try
        {
            filter = AccessibilityFilter.Build(AssemblyPath);
        }
        catch (Exception ex)
        {
            Log.LogError("XmlDocsToMarkdownTask: failed to read metadata from '{0}': {1}", AssemblyPath, ex.Message);
            return false;
        }

        var members = xml.Descendants("member")
            .Select(MemberDoc.Parse)
            .Where(static m => m is not null)
            .Select(static m => m!)
            .Where(filter.IsAllowed)
            .ToList();

        var byType = new Dictionary<string, List<MemberDoc>>(StringComparer.Ordinal);
        foreach (var member in members)
        {
            if (!byType.TryGetValue(member.OwningType, out var bucket))
            {
                bucket = [];
                byType[member.OwningType] = bucket;
            }
            bucket.Add(member);
        }

        Directory.CreateDirectory(OutputDirectory);

        var byRelativePath = new Dictionary<string, (string TypeFqn, List<MemberDoc> Members)>(StringComparer.Ordinal);
        foreach (var pair in byType)
        {
            byRelativePath[ToRelativeMarkdownPath(pair.Key)] = (pair.Key, pair.Value);
        }

        var writtenTypes = new HashSet<string>(byType.Keys, StringComparer.Ordinal);
        var namespaceIndexes = NamespaceIndexRenderer.RenderAll(byType, filter, writtenTypes);

        var assemblyIndexRelativePath = "index.md";
        var keepPaths = new HashSet<string>(byRelativePath.Keys, StringComparer.Ordinal) { assemblyIndexRelativePath };
        foreach (var nsIndex in namespaceIndexes.Keys)
        {
            keepPaths.Add(nsIndex);
        }
        ClearStaleMarkdown(OutputDirectory, keepPaths);

        var written = 0;
        foreach (var pair in byRelativePath)
        {
            var path = Path.Combine(OutputDirectory, pair.Key);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var content = MarkdownRenderer.Render(
                pair.Value.TypeFqn,
                pair.Value.Members,
                AssemblyName,
                filter,
                writtenTypes,
                ToForwardSlashRelativePath(pair.Key));
            File.WriteAllText(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            written++;
        }

        var indexContent = AssemblyIndexRenderer.Render(AssemblyName, byType.Keys);
        File.WriteAllText(
            Path.Combine(OutputDirectory, assemblyIndexRelativePath),
            indexContent,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        foreach (var nsIndex in namespaceIndexes)
        {
            var path = Path.Combine(OutputDirectory, nsIndex.Key.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, nsIndex.Value, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }

        WriteApiRootIndex();

        Log.LogMessage(MessageImportance.Normal,
            "XmlDocsToMarkdownTask: wrote {0} type page(s), {1} namespace index page(s), and the assembly index to '{2}' for '{3}'.",
            written, namespaceIndexes.Count, OutputDirectory, AssemblyName);
        return true;
    }

    private void WriteApiRootIndex()
    {
        var apiRoot = Path.GetDirectoryName(OutputDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (string.IsNullOrEmpty(apiRoot) || !Directory.Exists(apiRoot))
        {
            return;
        }

        var assemblyEntries = Directory.EnumerateDirectories(apiRoot)
            .Where(static dir => File.Exists(Path.Combine(dir, "index.md")))
            .Select(static dir => Path.GetFileName(dir))
            .Where(static name => !string.IsNullOrEmpty(name))
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToList();

        var output = new StringBuilder();
        output.AppendLine("# API Reference");
        output.AppendLine();
        output.AppendLine("Generated from XML doc comments. One entry per shipped assembly.");
        output.AppendLine();
        foreach (var name in assemblyEntries)
        {
            output.Append("- [").Append(name).Append("](").Append(name).AppendLine("/index.md)");
        }

        File.WriteAllText(
            Path.Combine(apiRoot, "index.md"),
            output.ToString(),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static string ToRelativeMarkdownPath(string typeFqn)
    {
        var lastDot = typeFqn.LastIndexOf('.');
        if (lastDot < 0)
        {
            return $"{typeFqn}.md";
        }
        var namespacePath = typeFqn.Substring(0, lastDot).Replace('.', Path.DirectorySeparatorChar);
        var typeName = typeFqn.Substring(lastDot + 1);
        return Path.Combine(namespacePath, $"{typeName}.md");
    }

    private static string ToForwardSlashRelativePath(string platformPath)
    {
        return platformPath.Replace(Path.DirectorySeparatorChar, '/');
    }

    private static void ClearStaleMarkdown(string directory, IEnumerable<string> keepRelativePaths)
    {
        var keep = new HashSet<string>(keepRelativePaths, StringComparer.Ordinal);
        foreach (var existing in Directory.EnumerateFiles(directory, "*.md", SearchOption.AllDirectories))
        {
            var relative = GetRelativePath(directory, existing);
            if (!keep.Contains(relative))
            {
                File.Delete(existing);
            }
        }

        RemoveEmptyDirectories(directory);
    }

    private static string GetRelativePath(string root, string fullPath)
    {
        var normalizedRoot = $"{root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)}{Path.DirectorySeparatorChar}";
        if (fullPath.StartsWith(normalizedRoot, StringComparison.Ordinal))
        {
            return fullPath.Substring(normalizedRoot.Length);
        }
        return fullPath;
    }

    private static void RemoveEmptyDirectories(string directory)
    {
        foreach (var sub in Directory.EnumerateDirectories(directory))
        {
            RemoveEmptyDirectories(sub);
            if (!Directory.EnumerateFileSystemEntries(sub).Any())
            {
                Directory.Delete(sub);
            }
        }
    }
}
