---
name: Atomic commits and full-solution build/test
description: Commit cadence and how to verify changes in LlamaShears
type: feedback
---

Make atomic commits as you go after each logical change — do not batch unrelated edits into a single commit.

Always run `dotnet build` and `dotnet test` with **no parameters** (no project filter, no `--filter`). Never build or test only the project you touched.

**Why:** User wants per-change traceability and the full solution must remain green at every commit. Project-scoped builds/tests can mask regressions in dependent projects.

**How to apply:** After each atomic change, run `dotnet build` then `dotnet test` (both no-args). If green, commit using Conventional Commits. Repeat per change, not per session.
