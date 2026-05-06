using LlamaShears.Core.Abstractions.Paths;
using LlamaShears.Core.Abstractions.PromptContext;
using LlamaShears.Core.Abstractions.Templating;
using Microsoft.Extensions.Options;

namespace LlamaShears.Core.PromptContext;

public sealed class FilesystemPromptContextProvider : IPromptContextProvider
{
    private const string TemplatesRelativePath = "workspace/system/context/PROMPT.md";
    private const string DefaultBundledRelativePath = "content/templates/workspace/system/context/PROMPT.md";

    private readonly IShearsPaths _paths;
    private readonly ITemplateRenderer _renderer;
    private readonly string _bundledPath;

    public FilesystemPromptContextProvider(
        IShearsPaths paths,
        ITemplateRenderer renderer,
        IOptions<FilesystemPromptContextOptions> options)
    {
        _paths = paths;
        _renderer = renderer;
        var configured = options.Value.BundledPath;
        _bundledPath = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(AppContext.BaseDirectory, DefaultBundledRelativePath)
            : configured;
    }

    public async ValueTask<string?> GetAsync(
        PromptContextParameters parameters,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        var templatesPath = Path.Combine(_paths.GetPath(PathKind.Templates), TemplatesRelativePath);
        var rendered = await _renderer
            .RenderAsync(templatesPath, parameters, cancellationToken)
            .ConfigureAwait(false);
        if (rendered is not null)
        {
            return rendered;
        }
        return await _renderer
            .RenderAsync(_bundledPath, parameters, cancellationToken)
            .ConfigureAwait(false);
    }
}
