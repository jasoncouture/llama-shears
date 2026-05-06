using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Provider.Ollama;

public record OllamaThoughtFragment(string Content) : IModelThoughtResponse;
