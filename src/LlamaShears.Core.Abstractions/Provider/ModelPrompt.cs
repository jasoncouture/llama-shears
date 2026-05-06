namespace LlamaShears.Core.Abstractions.Provider;

/// <summary>
/// Provider-agnostic prompt: an ordered list of <see cref="ModelTurn"/>
/// values destined for an <see cref="ILanguageModel"/>. Providers
/// translate it into their wire format; consumers do not see that
/// translation.
/// </summary>
/// <param name="Turns">Turns making up the prompt, in chronological order.</param>
public record ModelPrompt(IReadOnlyList<ModelTurn> Turns);
