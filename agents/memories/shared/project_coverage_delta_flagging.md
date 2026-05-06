---
name: PR coverage-delta auto-flagging is planned
description: Code coverage will be measured per PR and a decrease will flag the PR for review (non-blocking). Implication: do not add manual "tests" sections to PR templates or CONTRIBUTING.
type: project
---

The plan: code coverage is calculated per PR. A decrease in coverage flags the PR for review — non-blocking, just a signal that the change is moving the trend in the wrong direction. Not yet implemented.

**Why:** Direct application of mechanism-over-memory. Asking contributors to write "what tests were added" in a PR template is asking them to remember and self-report — a level-4 (convention) check in the PRINCIPLES.md ordering. The tooling can compute the delta directly, which is level-2 (fail loudly at the boundary) for regressions and level-3 (visible nudge) for non-regressions. The section was removed from the code PR template on 2026-04-28 in anticipation.

**How to apply:**
- Do not re-add a "Tests" / "Test plan" / "What you tested" section to `.github/PULL_REQUEST_TEMPLATE/code.md` or to CONTRIBUTING.md. The coverage tool is the answer.
- Until the coverage tool ships, "every commit must build and tests must pass" (per `feedback_build_and_commit.md`) is still the gate. The template change is forward-looking; the build/test rule is current.
- When the coverage tool lands, update CONTRIBUTING.md to point at it, and remove this memory (or update it to record the shipped state).
