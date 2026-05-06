namespace LlamaShears.Hosting;

public interface IShearsPaths
{
    string DataRoot { get; }

    string WorkspaceRoot { get; }

    string AgentsRoot { get; }

    string TemplatesRoot { get; }

    string GetAgentWorkspaceDefaultPath(string agentName);
}
