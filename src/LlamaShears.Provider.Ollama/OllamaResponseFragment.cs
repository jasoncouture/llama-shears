using LlamaShears.Provider.Abstractions;

namespace LlamaShears.Provider.Ollama;

public record OllamaResponseFragment(string Content, bool IsDone) : IModelResponseFragment;
