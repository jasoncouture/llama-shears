using LlamaShears.Core.Abstractions.Caching;
using LlamaShears.Core.Abstractions.SystemPrompt;
using Scriban;
using Scriban.Runtime;

namespace LlamaShears.Core.Templating;

public sealed class TemplateRenderer : ITemplateRenderer
{
    private readonly IFileParserCache<TemplateRenderer> _cache;

    public TemplateRenderer(IFileParserCache<TemplateRenderer> cache)
    {
        ArgumentNullException.ThrowIfNull(cache);
        _cache = cache;
    }

    public async ValueTask<string?> RenderAsync(
        string templatePath,
        IReadOnlyDictionary<string, object?> data,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templatePath);
        ArgumentNullException.ThrowIfNull(data);

        var template = await _cache.GetOrParseAsync(
            templatePath,
            templatePath,
            ParseAsync,
            cancellationToken);
        if (template is null)
        {
            return null;
        }

        var globals = new ScriptObject();
        foreach (var (key, value) in data)
        {
            globals.SetValue(key, value, readOnly: false);
        }
        TemplateFilters.Register(globals);
        return template.Render(globals);
    }

    private static async ValueTask<Template?> ParseAsync(Stream? stream, string path, CancellationToken cancellationToken)
    {
        if (stream is null)
        {
            return null;
        }

        using var reader = new StreamReader(stream);
        var source = await reader.ReadToEndAsync(cancellationToken);
        return Template.Parse(source, path);
    }
}
