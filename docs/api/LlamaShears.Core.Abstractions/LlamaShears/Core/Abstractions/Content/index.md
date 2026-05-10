# LlamaShears.Core.Abstractions.Content

## Types

- [Attachment](Attachment.md) — A non-text payload attached to a turn. Carried as base64 so the turn can be persisted and replayed verbatim without separate blob storage.
- [AttachmentKind](AttachmentKind.md) — Modality of a non-text payload attached to a turn. Today the framework recognizes only [AttachmentKind](AttachmentKind.md).`Image`; additional kinds will be added as providers gain support.

