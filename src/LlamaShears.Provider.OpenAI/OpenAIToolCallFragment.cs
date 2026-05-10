using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Provider.OpenAI;

internal sealed record OpenAiToolCallFragment(ToolCall Call) : IModelToolCallFragment;
