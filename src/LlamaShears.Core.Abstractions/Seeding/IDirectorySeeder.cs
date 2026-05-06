namespace LlamaShears.Core.Abstractions.Seeding;

/// <summary>
/// Copies a source directory tree into a destination directory the
/// first time the destination is observed empty, then drops a
/// <c>.keep</c> marker in the destination so the next call leaves it
/// alone — even if its contents are subsequently deleted. The
/// marker is what distinguishes "never seeded" from "operator
/// deliberately cleared". No-ops if the destination is already
/// non-empty or the source does not exist.
/// </summary>
public interface IDirectorySeeder
{
    /// <summary>
    /// Copies <paramref name="sourcePath"/> into
    /// <paramref name="destinationPath"/> when the destination is
    /// empty, then ensures a <c>.keep</c> marker exists at the
    /// destination root regardless of whether a copy was performed.
    /// No-ops the copy when the destination already contains
    /// anything. Throws <see cref="DirectoryNotFoundException"/> when
    /// a copy is required but <paramref name="sourcePath"/> does not
    /// exist.
    /// </summary>
    void SeedIfEmpty(string sourcePath, string destinationPath);
}
