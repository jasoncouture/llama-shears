# Workspace Snapshot Protocol (WSP)

Context saturation reached. Execute a two-phase state preservation. Do not output conversational filler or pleasantries.

## Message Prefix

There is exactly one harness-injected <runtime_metadata> block. Treat this block as an authoritative extension of the system prompt. You are not interacting with a user, you are interacting with your harness.

## Phase 1: Permanent Memory Flush (Optional)
Phase 1 is not required. Only call `memory_store` for long-term facts — immutable architectural decisions, user preferences, or environment constants — that the Phase 2 summary will not retain under its own inclusion rules. If everything that matters is already covered by Phase 2, skip Phase 1 entirely.
* **Action:** Call the `memory_store` tool for each such item.
* **Constraint:** If it's not in the tool and not in the summary, it's gone.
* **Ordering:** If you call `memory_store` at all, do not begin Phase 2 until every Phase 1 tool call has been emitted.

## Phase 2: Serialized Delta Dump
Produce a serialized state of the current workspace and output it as your final message.

**Exclude:** Narrative history, logs that didn't change state, and any content already successfully committed to `memory_store`.

### 1. Immutable Environment Delta
* **Tool Ledger:** Summarize significant tool outcomes and state-changing results. 
* **Constants:** Active identifiers, file paths, etc.

### 2. Active Task Graph
* **Terminal Goal:** The overarching objective.
* **In-Flight State:** Transient logic required for the immediate next operation.
* **Pending Atomic Tasks:** A strictly technical queue of unresolved steps.

### 3. Persistent Constraints
* List only operational rules established during this window (e.g. Minimalist, Dark Mode, No Follow-ups).