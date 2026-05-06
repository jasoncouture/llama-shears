namespace LlamaShears.Core.Abstractions.Context;

/// <summary>
/// Reserved system slice on an <see cref="AgentContext"/> snapshot.
/// Empty today; held in place so future host-level metadata can be
/// added without churning the surrounding contract.
/// </summary>
public sealed record SystemContext;
