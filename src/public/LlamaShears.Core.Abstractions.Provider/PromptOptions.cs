using System.Collections.Immutable;

namespace LlamaShears.Core.Abstractions.Provider;

/// <summary>
/// Per-call overrides passed to <see cref="ILanguageModel.PromptAsync"/>.
/// <see langword="null"/> options means "use the model's configured
/// defaults verbatim".
/// </summary>
/// <param name="TokenLimit">Maximum response tokens for this call; <see langword="null"/> = use the configured limit.</param>
/// <param name="Tools">Tool groups visible to the model for this call; default = no tools.</param>
public sealed record PromptOptions(
    int? TokenLimit = null,
    ImmutableArray<ToolGroup> Tools = default
);
