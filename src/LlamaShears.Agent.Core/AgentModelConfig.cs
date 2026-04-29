using LlamaShears.Provider.Abstractions;

namespace LlamaShears.Agent.Core;

public sealed record AgentModelConfig
{
    public required ModelIdentity Id { get; init; }

    public ThinkLevel Think { get; init; } = ThinkLevel.None;

    public int? ContextLength { get; init; }

    /// <summary>
    /// How long the provider should keep this model loaded after the
    /// last request. <see langword="null"/> defers to the provider's
    /// default; <see cref="TimeSpan.Zero"/> means unload immediately;
    /// any negative <see cref="TimeSpan"/> means "never unload".
    /// </summary>
    public TimeSpan? KeepAlive { get; init; }
}
