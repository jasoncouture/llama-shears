using Markdig;

namespace LlamaShears.Api.Web.Services;

internal static class MarkdownRenderer
{
    private static readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .DisableHtml()
        .Build();

    public static string ToHtml(string? source)
    {
        if (string.IsNullOrEmpty(source))
        {
            return string.Empty;
        }
        return Markdown.ToHtml(source, _pipeline);
    }
}
