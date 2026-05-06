namespace LlamaShears.Core;

public sealed class AgentTokenStoreOptions
{
    public TimeSpan TokenLifetime { get; set; } = TimeSpan.FromSeconds(30);

    public TimeSpan SweepInterval { get; set; } = TimeSpan.FromSeconds(10);
}
