using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Provider.Ollama;

public record OllamaCompletionFragment(int TokenCount) : IModelCompletionResponse;
