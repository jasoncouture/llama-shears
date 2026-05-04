---
name: Atomic commits and Husky-driven pre-commit verification
description: Commit cadence in LlamaShears and how Husky enforces test/build correctness on every commit
type: feedback
---

Make atomic commits as you go after each logical change — do not batch unrelated edits into a single commit.

**You don't need to pre-flight `dotnet test` (or `dotnet build`) before committing.** Husky runs the full test suite as a pre-commit hook; if anything is broken — compilation, analyzers, tests — the commit fails and the hook output tells you what to fix. The hook is the source of truth for "is this change ready to commit," so a manual run beforehand is optional, not required.

Run tests yourself when it actually saves time — e.g. you suspect the change is broken and want feedback before assembling a commit, or you're mid-refactor and want a sanity check before continuing. Don't run them as a ritual before every commit.

When the hook fails, fix the underlying issue, re-stage, and create a *new* commit. Do not `--amend` (a failed pre-commit means the commit didn't happen, so amend would target the prior one) and do not `--no-verify`.

Whether running pre-commit or mid-development: never use `--filter` or a project argument. Project-scoped runs mask regressions in dependent projects, and `dotnet test` already builds, so back-to-back `dotnet build` + `dotnet test` is redundant.

**Why:** User configured Husky specifically to make pre-commit verification automatic and ungameable. Manually running `dotnet test` before *every* commit is the same work twice and adds latency without adding safety. The hook gates the commit; your discretion gates whether iterating is faster with a manual check first.

**How to apply:**
- Stage the change. Commit. Trust the hook.
- Manual `dotnet test` / `dotnet build` is fine when it saves you a round-trip — e.g. catching a clear bug before letting the hook find it.
- If the hook fails: read the output, fix, re-stage, new commit.
- Never `--no-verify` (skipping the hook is the whole bug this is meant to prevent).
- Never run with `--filter` or a project argument.
- Never `git push` without explicit user instruction.
