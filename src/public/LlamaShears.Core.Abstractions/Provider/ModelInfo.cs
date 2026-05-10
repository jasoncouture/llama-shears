using LlamaShears.Core.Abstractions.Common;
namespace LlamaShears.Core.Abstractions.Provider;

/// <summary>
/// Catalog entry returned by a provider's
/// <see cref="IProviderFactory.ListModelsAsync"/>. Enough metadata for
/// callers to choose a model without actually loading it.
/// </summary>
/// <param name="ModelId">Provider-scoped identifier; pair with the provider name to get a globally unique <see cref="CompositeIdentity"/>.</param>
/// <param name="DisplayName">Human-readable name surfaced in UI.</param>
/// <param name="Description">Optional longer-form description; <see langword="null"/> when the provider does not supply one.</param>
/// <param name="SupportedInputs">Bit-set of input modalities this model accepts.</param>
/// <param name="SupportsReasoning">Whether the model exposes a thinking / chain-of-thought channel.</param>
/// <param name="MaxContextWindow">Maximum prompt-token budget for a single call.</param>
public record ModelInfo(
    string ModelId,
    string DisplayName,
    string? Description,
    SupportedInputType SupportedInputs,
    bool SupportsReasoning,
    int MaxContextWindow
);
