# LlamaShears.Core.Abstractions.Paths.ProtectionMode

Assembly: `LlamaShears.Core.Abstractions`

Protection modes a [ProtectedFile](ProtectedFile.md) rule may deny.
[ProtectionMode](ProtectionMode.md).`Execute` is reserved for future use.

## Fields

### `Delete`

Deletes against the protected path are denied.

### `Execute`

Reserved for future use; currently unenforced.

### `None`

No operations are protected by this rule.

### `Read`

Reads against the protected path are denied.

### `Write`

Writes (create/overwrite/append) against the protected path are denied.

