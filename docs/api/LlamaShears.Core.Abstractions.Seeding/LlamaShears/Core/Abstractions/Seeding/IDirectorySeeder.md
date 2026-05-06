# LlamaShears.Core.Abstractions.Seeding.IDirectorySeeder

Assembly: `LlamaShears.Core.Abstractions.Seeding`

Copies a source directory tree into a destination directory the
first time the destination is observed empty, then drops a
`.keep` marker in the destination so the next call leaves it
alone — even if its contents are subsequently deleted. The
marker is what distinguishes "never seeded" from "operator
deliberately cleared". No-ops if the destination is already
non-empty or the source does not exist.

## Methods

### `SeedIfEmpty`(string sourcePath, string destinationPath)

Copies `sourcePath` into
`destinationPath` when the destination is
empty, then ensures a `.keep` marker exists at the
destination root regardless of whether a copy was performed.
No-ops the copy when the destination already contains
anything. Throws DirectoryNotFoundException when
a copy is required but `sourcePath` does not
exist.

