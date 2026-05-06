namespace LlamaShears.Agent.Core.SystemPrompt;

public interface ITemplateRenderer
{
    string Render(string templatePath, object input);
}
