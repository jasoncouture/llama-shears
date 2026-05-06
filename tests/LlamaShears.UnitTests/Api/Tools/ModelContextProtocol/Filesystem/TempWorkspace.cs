using LlamaShears.Api.Tools.ModelContextProtocol.Filesystem;

namespace LlamaShears.UnitTests.Api.Tools.ModelContextProtocol.Filesystem;

internal sealed class TempWorkspace : IDisposable
{
    private TempWorkspace(string root, AgentWorkspace workspace)
    {
        Root = root;
        Workspace = workspace;
    }

    public string Root { get; }

    public AgentWorkspace Workspace { get; }

    public static TempWorkspace Create(string agentId = "test-agent")
    {
        var root = Path.Combine(Path.GetTempPath(), "llamashears-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var resolved = Path.GetFullPath(root);
        return new TempWorkspace(resolved, new AgentWorkspace(agentId, resolved));
    }

    public string PathOf(params string[] parts) => Path.Combine([Root, .. parts]);

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
        catch (IOException)
        {
            // best-effort cleanup; tests should not fail on transient FS issues
        }
    }
}
