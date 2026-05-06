---
name: Loud failures beat silent success
description: When a code path can only be triggered by a bug (or an attack we aren't guarding against), surface the failure loudly — don't fall through silently.
type: feedback
---

If a code path can only be reached because of a bug in our own code (or an attack we aren't pretending to defend against), it must surface loudly: an HTTP error, an exception, a failed assertion — not a "best effort" continue-as-if-nothing-happened.

**Why:** Silent success on a buggy path turns a debugging session into archaeology. The user has internalized this as part of pit-of-success: when the only realistic explanation for a state is "something is wrong," "guest"-style fall-through hides the wrongness. A 403 (or 500, or thrown exception) makes the bug obvious at the point of occurrence.

This applies *because* we aren't trying to defend against an adversary. In a hardened public-facing system, leaking "your token was rejected" via a 403 vs. "your token was accepted" via a 200 can be useful info for an attacker. We don't have that threat model. The only realistic source of an invalid token is "we have a bug minting/attaching tokens." Make it loud so we find and fix the bug.

**How to apply:**
- Distinguish "didn't try" (no input, anonymous OK) from "tried and failed" (bug or attack). The former should pass through quietly; the latter should not.
- Concrete example: invalid `Authorization: Bearer` returns 403; missing header returns anonymous. The difference is whether the caller *attempted* to authenticate.
- Don't silently coerce error states into success defaults. If a function would return a sentinel like "guest" / null / empty when something genuinely went wrong upstream, prefer to surface the failure instead.
- Reach for this principle when the realistic blast radius of "loud" is "a developer notices a bug faster" and the realistic blast radius of "quiet" is "someone debugs phantom behavior for hours."
