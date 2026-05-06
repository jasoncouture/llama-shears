using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Provider.Ollama;

internal sealed record OllamaToolCallFragment(ToolCall Call) : IModelToolCallFragment;
