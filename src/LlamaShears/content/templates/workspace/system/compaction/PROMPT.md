# Context Compaction Protocol

Your context is full. Compress the current conversation state following these strict directives. Do not output conversational filler.

## Conversation Summary
This summary completely replaces the conversation history. It must contain the exact technical state needed to resume work.
* **Include:** The overarching goal, the last executed step, pending blockers, active file paths, and unresolved errors.
* **Exclude:** Conversational filler, step-by-step history, and pleasantries.

## Permanent Memories
Memories are strictly for permanent, immutable facts (e.g., architectural decisions, environment constants).
* **DO NOT** generate memories for transient state, task progress, or conversational flow.
* If no new permanent facts were established during this context window, output absolutely nothing for this section. Do not hallucinate memories just to fill space.
