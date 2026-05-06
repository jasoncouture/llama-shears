# ADR-0008: No per-model workarounds

Accepted 2026-04-28.

## Context

Every LLM has rough edges. Some are unique to one model and one release; some are broadly shared across the family of autoregressive transformers people currently call "language models." A framework that hosts agents will see all of them. The question is which ones the framework absorbs and which ones it leaves to the user.

The naive answer — "fix everything we can" — is a treadmill. Per-model workarounds:

- Multiply with each model and each release. The framework gains a permanent maintenance burden that grows faster than the actual feature set.
- Become wrong when the underlying model changes. A workaround for a bug in version *N* often breaks behaviour in version *N+1* where the bug was fixed.
- Hide the model's actual behaviour from the user. A user reading well-behaved code may not realise the framework is silently masking a problem they need to know about.
- Encode someone else's bug into our public API surface, where it lives forever even after the bug is gone.

The opposite naive answer — "don't fix anything, the model's behaviour is the user's problem" — is also wrong. Some classes of problem are shared across essentially every model the framework will ever talk to: runaway tool-call loops, malformed JSON from "freeform JSON" prompts, models ignoring termination markers, models pretending tools succeeded when they didn't. These are not Gemma's problem or GPT-4o's problem; they are *language-model-shaped* problems. Ignoring them produces a framework that requires every user to re-discover and re-solve the same five issues before they get anything working.

The correct line is between *idiosyncratic* and *structural*.

## Decision

The framework absorbs problems that are broadly shared across language models. The framework does not absorb problems that are unique to one model.

**The framework will:**

- Implement structural safeguards against problem patterns that occur across the LLM family. Examples:
  - Iteration ceilings on agent loops, because every model occasionally loops on tool calls.
  - Structured-output mechanisms (e.g. tool-call as the way to extract reliable JSON), because every model is bad at freeform JSON.
  - Required-terminator patterns (e.g. `ReportStatus`), because every model occasionally produces text that *looks* terminal when it isn't.
- Expose general levers — configuration, prompts, callbacks, tools — that users can wire up to handle their specific model's quirks themselves.
- Add features when the question "do most models have this problem?" is yes.

**The framework will not:**

- Branch on model identity to apply per-model fixes (`if (modelName == "gemma:4b") ...`).
- Encode workarounds for a single model's release-specific behaviour into framework primitives.
- Add features whose justification is "model X does Y in version Z."

**When a single model has a problem, the answer is:**

1. The user identifies the problem.
2. The user reaches for a general lever the framework already exposes — adjusting prompts, tightening iteration limits, adding a tool, changing the agent's configuration, swapping models.
3. If no general lever fits, the *general lever itself* may be a candidate for a framework feature, but the justification is the missing capability, not the specific model's bug.

## Consequences

- The framework's surface area stays bounded by what's structurally useful, not by the cumulative bug history of every model the project has ever touched.
- Some per-model issues remain visible to users. That visibility is a feature: the user knows what their model does, and what they're choosing to live with or work around.
- Adding a feature requires answering "is this a class of problem most models share?" out loud. "Model X is broken in this way" is not by itself sufficient.
- The framework can remain useful to models that don't yet exist, because nothing in the framework assumes anything about a specific model's behaviour.
- Users who want a turnkey "fix Gemma's loop tendency" experience will not find it baked in. They will find iteration limits and structured-output primitives that make the problem manageable, applied at the agent-configuration level.

This ADR pairs with [ADR-0007 (Pit of success)](0007-pit-of-success.md): pit-of-success says the easy path should be the right path; this ADR says *what counts as the right path* is shaped by the language-model class of problem, not by any individual model's quirks.
