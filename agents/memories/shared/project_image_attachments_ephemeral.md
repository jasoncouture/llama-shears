---
name: Image attachments are not persisted
description: Images ride the inbound turn for this model call only; AppendAsync writes text without them; StripImageAttachments drops them from live context after infer
type: project
---

`Attachment` payloads (today `AttachmentKind.Image` only) are for the in-flight model call.

- `IAgentContext.AppendAsync` serializes a copy without image attachments. The in-memory snapshot keeps the original so this iteration's `Prompt` (built from `Turns` after persist) still has them.
- After the model has seen them, `IAgentContext.StripImageAttachments()` replaces those turns in memory. `RunIterationMiddleware` does this in a `finally` (covers infer throws). `StripImageAttachmentsMiddleware` (12000) does it again after tools.
- Intake (`ChannelMessageIntakeService`) still copies attachments onto the queued user turn.
- Replayed history / `ReadCurrentAsync` will not show images. That is intended.

Do not strip images from the in-memory list at append time — that would drop them from this turn's `Prompt`. Do not move persist to after infer (compaction-after-infer already dropped the assistant turn once).
