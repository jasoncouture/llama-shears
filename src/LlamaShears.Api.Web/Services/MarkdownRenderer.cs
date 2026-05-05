using Markdig;

namespace LlamaShears.Api.Web.Services;

internal static class MarkdownRenderer
{
    // DisableHtml strips raw <script>/<iframe>/etc. tags from the source
    // so a model writing inline HTML can't inject markup. Advanced
    // extensions get tables, fenced-code language hints, autolinks, and
    // task lists — the GitHub-flavored basics. Pipeline is cached
    // because constructing it isn't free and we render it on every
    // bubble paint.
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
