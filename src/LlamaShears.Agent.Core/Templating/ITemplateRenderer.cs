namespace LlamaShears.Agent.Core.Templating;

public interface ITemplateRenderer
{
    string Render(string templatePath, object input);
}
