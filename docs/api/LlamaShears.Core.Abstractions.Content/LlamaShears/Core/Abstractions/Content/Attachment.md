# LlamaShears.Core.Abstractions.Content.Attachment

Assembly: `LlamaShears.Core.Abstractions.Content`

A non-text payload attached to a turn. Carried as base64 so the
turn can be persisted and replayed verbatim without separate blob
storage.

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

A non-text payload attached to a turn. Carried as base64 so the
turn can be persisted and replayed verbatim without separate blob
storage.

#### Parameters

- `Kind` — Modality of the attachment.
- `MimeType` — MIME type of the payload (`image/png`, `image/jpeg`, …).
- `Base64Data` — Payload encoded as base64.

