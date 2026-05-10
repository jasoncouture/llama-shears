namespace LlamaShears.Core.Abstractions.Paths;

/// <summary>
/// Filesystem entry kind for path protection rules. <see cref="Special"/>
/// covers sockets, FIFOs, devices, and other non-regular non-directory
/// entries. Reserved for future filtering; defaults treat them as
/// non-matching for file-only rules.
/// </summary>
[Flags]
public enum FileType
{
    /// <summary>No filesystem kind — sentinel for "rule does not apply".</summary>
    None = 0,
    /// <summary>Regular file.</summary>
    File = 1,
    /// <summary>Directory entry.</summary>
    Directory = 2,
    /// <summary>Sockets, FIFOs, devices, and other non-regular non-directory entries.</summary>
    Special = 4,
    /// <summary>Combined match for every concrete kind: <see cref="File"/>, <see cref="Directory"/>, and <see cref="Special"/>.</summary>
    Any = File | Directory | Special,
}
