namespace LlamaShears.Provider.Abstractions;

/// <summary>
/// Types of input a model may support.
/// </summary>
[Flags]
public enum SupportedInputType
{
    None = 0,
    Text = 1 << 0,
    Image = 1 << 1,
    Audio = 1 << 2,
    Video = 1 << 3
}
