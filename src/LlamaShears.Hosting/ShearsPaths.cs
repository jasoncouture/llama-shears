using Microsoft.Extensions.Options;

namespace LlamaShears.Hosting;

public sealed class ShearsPaths : IShearsPaths
{
    private const string DefaultDataRootName = ".llama-shears";
    private const string DefaultWorkspaceFolderName = "workspace";
    private const string DefaultAgentsFolderName = "agents";
    private const string DefaultTemplatesFolderName = "templates";

    public ShearsPaths(IOptions<ShearsPathsOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var values = options.Value;

        DataRoot = ResolveAndCreate(values.DataRoot, DefaultDataRoot);
        WorkspaceRoot = ResolveAndCreate(values.WorkspaceRoot, () => Path.Combine(DataRoot, DefaultWorkspaceFolderName));
        AgentsRoot = ResolveAndCreate(values.AgentsRoot, () => Path.Combine(DataRoot, DefaultAgentsFolderName));
        TemplatesRoot = ResolveAndCreate(values.TemplatesRoot, () => Path.Combine(DataRoot, DefaultTemplatesFolderName));
    }

    public string DataRoot { get; }

    public string WorkspaceRoot { get; }

    public string AgentsRoot { get; }

    public string TemplatesRoot { get; }

    public string GetAgentWorkspaceDefaultPath(string agentName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentName);
        return Path.Combine(WorkspaceRoot, agentName);
    }

    private static string DefaultDataRoot() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            DefaultDataRootName);

    private static string ResolveAndCreate(string? configured, Func<string> defaultPathFactory)
    {
        var path = string.IsNullOrWhiteSpace(configured) ? defaultPathFactory() : configured;
        return Directory.CreateDirectory(path).FullName;
    }
}
