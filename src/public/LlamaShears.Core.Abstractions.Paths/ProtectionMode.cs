namespace LlamaShears.Core.Abstractions.Paths;

/// <summary>
/// Protection modes a <see cref="ProtectedFile"/> rule may deny.
/// <see cref="Execute"/> is reserved for future use.
/// </summary>
[Flags]
public enum ProtectionMode
{
    None = 0,
    Read = 1,
    Write = 2,
    Delete = 4,
    Execute = 8,
}
