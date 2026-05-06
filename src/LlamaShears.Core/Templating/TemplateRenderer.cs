using LlamaShears.Core.Abstractions.Caching;
using LlamaShears.Core.Abstractions.Templating;
using Scriban;

namespace LlamaShears.Core.Templating;

public sealed class TemplateRenderer : ITemplateRenderer
{
    private readonly IFileParserCache<TemplateRenderer> _cache;

    public TemplateRenderer(IFileParserCache<TemplateRenderer> cache)
    {
        ArgumentNullException.ThrowIfNull(cache);
        _cache = cache;
    }

    public string Render(string templatePath, object input)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templatePath);
        ArgumentNullException.ThrowIfNull(input);

        var template = _cache.GetOrParseAsync<Template, string>(
            templatePath,
            templatePath,
            ParseAsync,
            CancellationToken.None).GetAwaiter().GetResult()
            ?? throw new FileNotFoundException(
                $"Template not found: {templatePath}", templatePath);

        return template.Render(input);
    }

    private static async ValueTask<Template?> ParseAsync(Stream? stream, string path, CancellationToken cancellationToken)
    {
        if (stream is null)
        {
            return null;
        }

        using var reader = new StreamReader(stream);
        var source = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        return Template.Parse(source, path);
    }
}
