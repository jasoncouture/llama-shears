---
name: Atomic commits and full-solution build/test
description: Commit cadence and how to verify changes in LlamaShears (including when verification is skippable)
type: feedback
---

Make atomic commits as you go after each logical change — do not batch unrelated edits into a single commit.

**Decide whether tests are needed by inspecting the staged change set, not by reflex.** Run `git diff --cached --name-only` (or equivalent) and look at the file extensions:

- If *any* file in the change set can affect compilation, analyzers, or test execution — `*.cs`, `*.csproj`, `*.slnx`, `*.props`, `*.targets`, `global.json`, `Directory.Build.*`, `nuget.config`, `*.editorconfig`, `*.resx`, analyzer release-tracking files, anything under `analyzers/` — run tests.
- If the change set is purely documentation or non-build artifacts (`*.md`, `LICENSE*`, images, anything under `docs/`, `agents/memories/`, README/CONTRIBUTING/etc.) — **skip tests**. Running them is goofy and wastes time. The compiler doesn't read markdown.

Verification when tests are needed: run `dotnet test` with **no parameters** (no project filter, no `--filter`). Never test only the project you touched.

`dotnet test` runs `dotnet build` as part of its pipeline; running both back-to-back is redundant. Only run `dotnet build` on its own for a compile-only check (mid-refactor, when you don't yet care whether tests pass).

**Why:** Two corrections from the user. First: project-scoped builds/tests can mask regressions in dependent projects, and `dotnet test` already builds, so the "always full solution, no filters" policy stands. Second (2026-04-28): running `dotnet test` before a doc-only commit is busywork — the rule is to verify *changes that could break the build*, not to run tests as a ritual before every commit. The check is "does the diff touch anything the compiler or test runner cares about?" If no, skip.

**How to apply:**
- Inspect the staged set first. If it's docs-only, commit without testing.
- If any build-relevant file is staged, default verification: `dotnet test` (no args).
- Mid-refactor compile-only check: `dotnet build` (no args).
- Never both back-to-back; never with `--filter` or a project argument.
- If green (or skipped under the docs-only rule), commit using Conventional Commits. Repeat per change, not per session.
