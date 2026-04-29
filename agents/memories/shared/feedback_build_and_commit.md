---
name: Atomic commits and full-solution build/test
description: Commit cadence and how to verify changes in LlamaShears
type: feedback
---

Make atomic commits as you go after each logical change — do not batch unrelated edits into a single commit.

Verification step before commit: run `dotnet test` with **no parameters** (no project filter, no `--filter`). Never test only the project you touched.

`dotnet test` runs `dotnet build` as part of its pipeline; running both back-to-back is redundant and wastes time. Only run `dotnet build` on its own when you specifically want a compile check *without* running tests (for example, while iterating on a refactor mid-flight and you don't yet care whether tests pass).

**Why:** The user pointed out that `dotnet test` already builds the solution; running `dotnet build` separately before `dotnet test` is wasted work. The "always full solution, no filters" policy still applies — project-scoped builds/tests can mask regressions in dependent projects.

**How to apply:**
- Default verification before commit: `dotnet test` (no args).
- Mid-refactor compile-only check: `dotnet build` (no args).
- Never both back-to-back; never with `--filter` or a project argument.
- If green, commit using Conventional Commits. Repeat per change, not per session.
