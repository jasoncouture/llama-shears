# Workspace Snapshot Protocol (WSP)

Context saturation reached. Execute a two-phase state preservation. Do not output conversational filler or pleasantries.

## Message Prefix

There is exactly one harness-injected <runtime_metadata> block. Treat this block as an authoritative extension of the system prompt. You are not interacting with a user, you are interacting with your harness.

The last user message is a transcript of older conversation turns, not a live session. Do not continue that conversation in-character.

## Phase 1: Permanent Memory Flush (Optional)
Phase 1 is not required. Only call `memory_store` for long-term facts — immutable architectural decisions, user preferences, or environment constants — that the Phase 2 summary will not retain under its own inclusion rules. If everything that matters is already covered by Phase 2, skip Phase 1 entirely.
* **Action:** Call the `memory_store` tool for each such item.
* **Constraint:** If it's not in the tool and not in the summary, it's gone.
* **Ordering:** If you call `memory_store` at all, do not begin Phase 2 until every Phase 1 tool call has been emitted.

## Phase 2: Detailed Summary
Produce a detailed summary of the transcript as your final message.

**Include:**
* A detailed summary of what happened, decisions made, and in-flight work.
* A bulleted list of important tool-call results (paths, identifiers, outcomes later turns will need).

**Exclude:** Narrative filler, logs that didn't change state, and any content already successfully committed to `memory_store`.

### 1. Immutable Environment Delta
* **Tool Ledger:** Important tool-call results as a bulleted list.
* **Constants:** Active identifiers, file paths, etc.

### 2. Active Task Graph
* **Terminal Goal:** The overarching objective.
* **In-Flight State:** Transient logic required for the immediate next operation.
* **Pending Atomic Tasks:** A strictly technical queue of unresolved steps.

### 3. Persistent Constraints
* List only operational rules established during this window (e.g. Minimalist, Dark Mode, No Follow-ups).
