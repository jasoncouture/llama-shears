namespace LlamaShears.Hosting;

public sealed class ShearsPathsOptions
{
    public string? DataRoot { get; set; }

    public string? WorkspaceRoot { get; set; }

    public string? AgentsRoot { get; set; }

    public string? TemplatesRoot { get; set; }

    public string? ConversationsRoot { get; set; }
}
