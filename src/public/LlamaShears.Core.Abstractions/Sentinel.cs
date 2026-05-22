namespace LlamaShears.Core.Abstractions;

/// <summary>
/// Well-known sentinel strings used to signal special conditions in
/// model output that wouldn't survive cleanly as a typed value.
/// </summary>
public static class Sentinel
{
    /// <summary>
    /// Emitted by the model when it has nothing to say. The inference
    /// runner treats this as "suppress the turn" rather than forwarding
    /// the literal text on to channels.
    /// </summary>
    public const string NoResponse = "NO_RESPONSE";
}
