using LlamaShears.Provider.Abstractions;

namespace LlamaShears.Provider.Ollama;

public record OllamaThoughtFragment(string Content) : IModelThoughtResponse;
