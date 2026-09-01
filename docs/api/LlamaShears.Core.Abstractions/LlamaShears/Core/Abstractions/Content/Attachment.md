# LlamaShears.Core.Abstractions.Content.Attachment

Assembly: `LlamaShears.Core.Abstractions`

A non-text payload attached to a turn. Carried as base64 for
in-flight delivery to the language model. Image attachments are
not written to the context store and are stripped from live
context after the model has seen them.

## Parameters

- `Kind` — Modality of the attachment.
- `MimeType` — MIME type of the payload (`image/png`, `image/jpeg`, …).
- `Base64Data` — Payload encoded as base64.

## Properties

### `Base64Data`

Payload encoded as base64.

### `Kind`

Modality of the attachment.

### `MimeType`

MIME type of the payload (`image/png`, `image/jpeg`, …).

## Methods

### `Attachment`([AttachmentKind](AttachmentKind.md) Kind, string MimeType, string Base64Data)

A non-text payload attached to a turn. Carried as base64 for
in-flight delivery to the language model. Image attachments are
not written to the context store and are stripped from live
context after the model has seen them.

#### Parameters

- `Kind` — Modality of the attachment.
- `MimeType` — MIME type of the payload (`image/png`, `image/jpeg`, …).
- `Base64Data` — Payload encoded as base64.

