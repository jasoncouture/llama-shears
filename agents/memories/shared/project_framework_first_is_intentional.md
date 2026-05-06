---
name: Framework-first is the intentional sequencing
description: The substrate-before-agent-loop ratio is deliberate; do not critique it as over-engineering or scaffolding-heavy in evaluations.
type: project
---

The scaffolding-to-feature ratio in this repo (heartbeat tick, provider abstractions, agent token store, MCP wiring, EF+hooks layer, analyzers) is built out *before* a working agent loop on purpose. An agent needs tools, communication, recurring time/poke/prod, and possibly multi-agent interaction to function at all — those are *what an agent is made of*, not bolt-ons. Each substrate piece is built and validated on its own ("what piece do I need next, validatable in isolation?") to avoid mass refactor debt later.

**Why:** Stated by the developer 2026-04-28 in response to my "scaffolding-to-feature ratio is high" critique. Retrofitting the substrate around a working agent later is the failure mode this is explicitly avoiding.

**How to apply:** When evaluating progress, do not measure "scaffolding vs. features built" as a quality signal. The right question is "is the next-needed substrate piece in flight, and is it validatable on its own?" If yes, the direction is correct even if the visible feature surface is small. Don't recommend skipping substrate work to ship an end-to-end demo earlier.
