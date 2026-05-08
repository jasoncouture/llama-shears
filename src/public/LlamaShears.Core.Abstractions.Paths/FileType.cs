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
    None = 0,
    File = 1,
    Directory = 2,
    Special = 4,
    Any = File | Directory | Special,
}
