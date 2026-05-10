namespace LlamaShears.Core.Abstractions.Paths;

/// <summary>
/// Protection modes a <see cref="ProtectedFile"/> rule may deny.
/// <see cref="Execute"/> is reserved for future use.
/// </summary>
[Flags]
public enum ProtectionMode
{
    /// <summary>No operations are protected by this rule.</summary>
    None = 0,
    /// <summary>Reads against the protected path are denied.</summary>
    Read = 1,
    /// <summary>Writes (create/overwrite/append) against the protected path are denied.</summary>
    Write = 2,
    /// <summary>Deletes against the protected path are denied.</summary>
    Delete = 4,
    /// <summary>Reserved for future use; currently unenforced.</summary>
    Execute = 8,
}
