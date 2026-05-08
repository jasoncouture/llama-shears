# LlamaShears.Core.Abstractions.SystemPrompt.ITemplateFileLocator

Assembly: `LlamaShears.Core.Abstractions.SystemPrompt`

Resolves a template file across the standard layered lookup:
per-workspace customization first, then operator-supplied templates,
then the bundled defaults that ship with the host. Returns the full
path of the first file that exists, or `null` if no
candidate hits.

## Methods

### `Locate`(string subFolder, string fileName, string defaultFileName)

Locate `fileName` (e.g. `"COMPACTION.md"`)
inside the optional `subFolder`; on miss, fall
back to `defaultFileName` at the same layer
before moving on to the next.

