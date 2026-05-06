using Scriban;

namespace LlamaShears.Agent.Core.SystemPrompt;

public sealed class TemplateRenderer : ITemplateRenderer
{
    public string Render(string templatePath, object input)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templatePath);
        ArgumentNullException.ThrowIfNull(input);

        var source = File.ReadAllText(templatePath);
        var template = Template.Parse(source, templatePath);
        return template.Render(input);
    }
}
