namespace LlamaShears.Agent.Core;

/// <summary>
/// Global toggle for the <see cref="SystemTickService"/>. When disabled
/// the service simply skips publishing system ticks; the hosted service
/// itself remains running.
/// </summary>
public sealed class SystemTickOptions
{
    /// <summary>
    /// When <see langword="false"/>, the system-tick service skips
    /// publishing on every tick. Defaults to <see langword="true"/>.
    /// </summary>
    public bool Enabled { get; set; } = true;
}
