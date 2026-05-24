using System.Collections;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using LlamaShears.Core.Abstractions.PromptContext;
using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Syntax;
using YamlDotNet.Serialization;

namespace LlamaShears.Core.PromptContext;

public sealed partial class SkillParser : ISkillParser
{
    private const int NameMinLength = 1;
    private const int NameMaxLength = 64;
    private const int DescriptionMinLength = 1;
    private const int DescriptionMaxLength = 1024;

    private static readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .UseYamlFrontMatter()
        .Build();

    private static readonly IDeserializer _yaml = new DeserializerBuilder().Build();

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex NameRegex();

    public SkillRecord? Parse(string documentText, string filePath)
    {
        ArgumentNullException.ThrowIfNull(documentText);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var parentDirectory = Path.GetDirectoryName(filePath);
        if (string.IsNullOrEmpty(parentDirectory))
            throw new InvalidOperationException(
                "Skill file path did not resolve to a directory, and this is required");
        var parentDirectoryName = Path.GetFileName(parentDirectory);

        var document = Markdown.Parse(documentText, _pipeline);
        if (document.FirstOrDefault() is not YamlFrontMatterBlock block) return null;

        var frontmatterText = block.Lines.ToString();
        var yamlDocument = _yaml.Deserialize<Dictionary<string, object?>>(frontmatterText);
        if (!yamlDocument.TryGetValue("name", out var nameObj))
            throw new InvalidOperationException("A name property is required in the document frontmatter");
        if (!yamlDocument.TryGetValue("description", out var descriptionObj))
            throw new InvalidOperationException("A description property is required in the document frontmatter");
        if (nameObj is not string name) throw new InvalidOperationException("The name property must be a string");
        if (descriptionObj is not string description)
            throw new InvalidOperationException("The description property must be a string");

        ValidateName(name, parentDirectoryName);
        ValidateDescription(description);

        ImmutableDictionary<string, object?> metadata = ImmutableDictionary<string, object?>.Empty
            .WithComparers(StringComparer.OrdinalIgnoreCase);
        if (yamlDocument.TryGetValue("metadata", out var metadataObject) && metadataObject is not null)
        {
            if (metadataObject is not IDictionary mapping)
                throw new InvalidOperationException("If a metadata property is present, it must be an object");
            metadata = ToStringKeyedDictionary(mapping);
        }

        var body = documentText[(block.Span.End + 1)..].TrimStart();
        var frontmatterExtraProperties = yamlDocument
            .ExceptBy(["name", "description", "metadata"], i => i.Key, StringComparer.OrdinalIgnoreCase)
            .ToImmutableDictionary(StringComparer.OrdinalIgnoreCase);

        return new SkillRecord(
            name,
            description,
            filePath,
            body,
            frontmatterExtraProperties,
            metadata);
    }

    private static void ValidateName(string name, string parentDirectoryName)
    {
        if (name.Length is < NameMinLength or > NameMaxLength)
            throw new InvalidOperationException(
                $"The name property must be {NameMinLength}-{NameMaxLength} characters long");
        if (!NameRegex().IsMatch(name))
            throw new InvalidOperationException(
                "The name property must contain only lowercase alphanumeric characters and hyphens, must not start or end with a hyphen, and must not contain consecutive hyphens");
        if (!string.Equals(name, parentDirectoryName, StringComparison.Ordinal))
            throw new InvalidOperationException(
                $"The name property '{name}' must match the parent directory name '{parentDirectoryName}'");
    }

    private static void ValidateDescription(string description)
    {
        if (description.Length is < DescriptionMinLength or > DescriptionMaxLength)
            throw new InvalidOperationException(
                $"The description property must be {DescriptionMinLength}-{DescriptionMaxLength} characters long");
    }

    private static ImmutableDictionary<string, object?> ToStringKeyedDictionary(IDictionary mapping)
    {
        var builder = ImmutableDictionary.CreateBuilder<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (DictionaryEntry entry in mapping)
        {
            if (entry.Key is not string key)
                throw new InvalidOperationException("Metadata keys must be strings");
            builder[key] = entry.Value;
        }
        return builder.ToImmutable();
    }
}
