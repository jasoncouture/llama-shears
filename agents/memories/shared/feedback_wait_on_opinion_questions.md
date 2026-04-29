---
name: Wait for approval after opinion questions
description: When the user asks "what do you think?" / "perhaps we should…", give the opinion and stop — even in auto mode
type: feedback
---

When the user asks an opinion question — phrasings like "what do you think?", "perhaps we should X", "should we Y instead?", "maybe we should Z?" — give the requested opinion and **stop**. Do not also implement the change in the same turn. Wait for an explicit "go" / "do it" / "apply" / approval before making code changes.

This applies even when auto mode is active. Auto mode's "prefer action" rule is for routine work where the next step is implied; opinion-solicitations are explicitly a request for discussion, and the user has not yet asked for the change.

**Why:** The user stated this directly: "In the future, when I ask you a question like that, wait for approval before making changes. In this case you did the right thing, but far too often you do this and do the wrong thing." The downside of waiting is one extra exchange when the answer would have been "yes"; the downside of jumping is unwanted changes the user has to undo. The asymmetry favors waiting.

**How to apply:**
- Pattern-match the *form* of the request: questions seeking judgment, hedged with "perhaps" / "maybe" / "what do you think" / "I'm wondering" / "should we", are opinion solicitations.
- Respond with the opinion, the reasoning, and (if useful) a concrete sketch of what the change would be — but do not edit files, run commands that mutate state, or otherwise execute the proposal.
- An opinion-question that is *also* paired with an explicit instruction ("…and go ahead and apply it") is a normal action request; obey the instruction.
- When unsure whether a message is an opinion question or an action request, treat it as an opinion question. Wait. Cost of asking is low; cost of unwanted changes is higher.
