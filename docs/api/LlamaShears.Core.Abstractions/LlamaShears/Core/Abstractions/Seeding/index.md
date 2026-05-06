# LlamaShears.Core.Abstractions.Seeding

## Types

- [IDirectorySeeder](IDirectorySeeder.md) — Copies a source directory tree into a destination directory the first time the destination is observed empty, then drops a `.keep` marker in the destination so the next call leaves it alone — even if its contents are subsequently deleted. The marker is what distinguishes "never seeded" from "operator deliberately cleared". No-ops if the destination is already non-empty or the source does not exist.

