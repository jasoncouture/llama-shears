# LlamaShears.Core.Abstractions.Paths

## Types

- [FileType](FileType.md) — Filesystem entry kind for path protection rules. [FileType](FileType.md).`Special` covers sockets, FIFOs, devices, and other non-regular non-directory entries. Reserved for future filtering; defaults treat them as non-matching for file-only rules.
- [IApplicationPathProvider](IApplicationPathProvider.md) — Resolves on-disk paths for the well-known categories of host state ([PathKind](PathKind.md)). Implementations decide where each root lives and whether to create directories on demand.
- [IFileProtectionPolicy](IFileProtectionPolicy.md) — Decides whether a workspace-relative path is protected from a requested operation. Implementations evaluate the registered set of [ProtectedFile](ProtectedFile.md) rules.
- [IPathExpander](IPathExpander.md) — Expands a possibly-shorthand path to an absolute path.
- [PathKind](PathKind.md) — Well-known categories of host state whose on-disk root is resolved by [IApplicationPathProvider](IApplicationPathProvider.md). Implementations decide where each root lives and whether to create directories on demand.
- [ProtectedFile](ProtectedFile.md) — Declares a protection rule for paths inside an agent workspace.
- [ProtectionMode](ProtectionMode.md) — Protection modes a [ProtectedFile](ProtectedFile.md) rule may deny. [ProtectionMode](ProtectionMode.md).`Execute` is reserved for future use.

