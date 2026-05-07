using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Provider.OpenAI;

internal sealed record OpenAIToolCallFragment(ToolCall Call) : IModelToolCallFragment;
