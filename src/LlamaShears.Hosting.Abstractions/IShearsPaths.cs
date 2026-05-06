namespace LlamaShears.Hosting;

public interface IShearsPaths
{
    string DataRoot { get; }

    string WorkspaceRoot { get; }

    string AgentsRoot { get; }

    string TemplatesRoot { get; }

    string GetPath(PathKind kind, string? subpath = null);

    string GetAgentWorkspaceDefaultPath(string agentName);
}
