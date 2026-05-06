using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Provider.Ollama;

public record OllamaResponseFragment(string Content) : IModelTextResponse;
