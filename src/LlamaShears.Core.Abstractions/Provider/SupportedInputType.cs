namespace LlamaShears.Core.Abstractions.Provider;

/// <summary>
/// Bit-set describing the modalities a model accepts as input. Used
/// by the catalog (<see cref="ModelInfo.SupportedInputs"/>) so
/// callers can route prompts containing attachments to the right
/// model without round-tripping the provider.
/// </summary>
[Flags]
public enum SupportedInputType
{
    /// <summary>No modalities are supported.</summary>
    None = 0,
    /// <summary>Plain text input is supported.</summary>
    Text = 1 << 0,
    /// <summary>Image attachments are supported.</summary>
    Image = 1 << 1,
    /// <summary>Audio attachments are supported.</summary>
    Audio = 1 << 2,
    /// <summary>Video attachments are supported.</summary>
    Video = 1 << 3
}
