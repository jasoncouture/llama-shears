---
name: Chat UI busy indicators
description: Message-list chips for compacting, thinking, tools, and processing; IsProcessing covers the pre-fragment gap
type: project
---

`MessageList` shows at most one footer busy chip, in this order:

1. **compacting** — `IChatSession.IsCompacting`. Always, even with an empty transcript.
2. **hidden stream** — thinking / running tools / typing when a toggle hides the live bubble.
3. **processing** — `IChatSession.IsProcessing`. After `SendAsync` (or inbound `agent:busy`) until `agent:idle`. This is the wait before the first thought, tool, or assistant fragment (model HTTP can take seconds).

Set `IsProcessing` in `SendAsync` as soon as the user bubble is appended — do not wait for `agent:busy`. Clear on idle, failed publish, or session change. Slash commands do not flip it. Compacting and a visible streaming bubble still win over the processing chip.
