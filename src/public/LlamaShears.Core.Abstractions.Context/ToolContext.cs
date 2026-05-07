using System.Collections.Immutable;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Core.Abstractions.Context;

/// <summary>
/// The flat tool catalog visible to the agent on an
/// <see cref="AgentContext"/> snapshot. The grouped form
/// (<see cref="ToolGroup"/>) is for prompts; this flat form is what
/// templates and tools iterate over.
/// </summary>
/// <param name="Items">Tools available, in registration order.</param>
public sealed record ToolContext(ImmutableArray<ToolDescriptor> Items);
