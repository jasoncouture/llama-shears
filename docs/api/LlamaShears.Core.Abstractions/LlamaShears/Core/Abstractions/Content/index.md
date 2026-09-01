# LlamaShears.Core.Abstractions.Content

## Types

- [Attachment](Attachment.md) — A non-text payload attached to a turn. Carried as base64 for in-flight delivery to the language model. Image attachments are not written to the context store and are stripped from live context after the model has seen them.
- [AttachmentKind](AttachmentKind.md) — Modality of a non-text payload attached to a turn. Today the framework recognizes only [AttachmentKind](AttachmentKind.md).`Image`; additional kinds will be added as providers gain support.

