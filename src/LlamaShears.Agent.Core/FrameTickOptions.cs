namespace LlamaShears.Agent.Core;

/// <summary>
/// Global toggle for the <see cref="FrameTickService"/>. When disabled
/// the service simply skips publishing frame ticks; the hosted service
/// itself remains running.
/// </summary>
public sealed class FrameTickOptions
{
    /// <summary>
    /// When <see langword="false"/>, the frame-tick service skips
    /// publishing on every tick. Defaults to <see langword="true"/>.
    /// </summary>
    public bool Enabled { get; set; } = true;
}
