namespace LlamaShears.Core.Abstractions.Provider;

/// <summary>
/// Marker contract for one piece of an <see cref="ILanguageModel"/>'s
/// streaming response. Concrete fragment shapes — visible text, hidden
/// reasoning — implement the more specific
/// <see cref="IModelTextResponse"/> or <see cref="IModelThoughtResponse"/>.
/// </summary>
public interface IModelResponseFragment
{
}
