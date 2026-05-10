# LlamaShears.Core.Abstractions.Paths.IPathExpander

Assembly: `LlamaShears.Core.Abstractions`

Expands a possibly-shorthand path to an absolute path.

## Methods

### `ExpandPath`(string path, string workingDirectory)

Expands `path` to an absolute path:

- If `path` is absolute, it is returned unchanged.
- If `path` is a bare `~` or begins with `~/`, the `~` is replaced with the current user's profile directory.
- Otherwise, `path` is joined with `workingDirectory` and resolved via Path.`GetFullPath`.

