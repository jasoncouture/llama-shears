# LlamaShears.Core.Abstractions.Content

Message-content primitives for [LlamaShears](https://github.com/jasoncouture/llama-shears). Today this is just attachments — the shared shape used by both the event pipeline (`ChannelMessage`) and the model surface (`ModelTurn`) so a single attachment can flow from UI → bus → provider without re-shaping.

## Public surface

- **`Attachment`** — name + MIME + base64 payload + optional id, with a `Kind` discriminator.
- **`AttachmentKind`** — content-class tag (today: `Image`; more types are tracked in [TASKS.md](https://github.com/jasoncouture/llama-shears/blob/main/TASKS.md)).

## See also

- [Architecture overview](https://github.com/jasoncouture/llama-shears/blob/main/docs/design/architecture.md)
- [LlamaShears on GitHub](https://github.com/jasoncouture/llama-shears)

## Licensing

[AGPL-3.0-or-later](https://github.com/jasoncouture/llama-shears/blob/main/LICENSE.md). [Commercial licensing](https://github.com/jasoncouture/llama-shears/blob/main/COMMERCIAL.md) is available.
