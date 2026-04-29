using LlamaShears.Provider.Abstractions;

namespace LlamaShears.Provider.Ollama;

/// <summary>
/// A fragment of a streamed Ollama chat response.
/// </summary>
public record OllamaResponseFragment(string Content, bool IsDone) : IModelResponseFragment;
