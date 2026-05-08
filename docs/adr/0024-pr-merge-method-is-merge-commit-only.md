# ADR-0024: PR merges use merge commits only — never squash, never rebase

Accepted 2026-05-07.

## Context

GitHub offers three merge methods on a PR: **merge commit**, **squash**, and **rebase**. They look interchangeable from the button — they're not. Each writes a structurally different shape of history onto `main`, and the choice has long-tail consequences for the operations that ride on top of that history (revert, bisect, blame, PR enumeration, review of in-flight PRs).

### Squash

`Squash and merge` collapses every commit on the PR branch into a single new commit on `main`.

- **Granularity is gone.** A 30-commit PR with one independently broken commit (commit 16) lands on `main` as one blob. `git revert <commit-16-sha>` is impossible — that SHA isn't on `main`. To unship just that change you have to find the original branch (assumes the branch wasn't deleted, the GC hasn't run, or the reflog still carries it), extract the diff for commit 16 by hand, and reverse-apply it as a new commit while resolving conflicts against the other 29 commits' changes that *did* land. A 30-second `git revert` becomes a multi-hour reconstruction job.
- **Bisect granularity is gone.** Every probe lands on the same squash commit. The smallest blame footprint bisect can produce is "the entire PR."
- **PR boundary is text, not topology.** GitHub appends `(#NNN)` to the squash commit's subject by default, but the merge message is editable on merge. A custom subject defeats the reference. The boundary is a hint, not a structural marker.

### Rebase

`Rebase and merge` fast-forwards the PR branch's individual commits linearly onto `main`. There is **no merge commit** at all.

- **No PR boundary, period.** `git log --first-parent` is identical to `git log` — every commit is on first parent. There is nothing structural to enumerate PRs by. GitHub may append `(#NNN)` to commit subjects, but again that's editable text in commit messages, not topology. Custom commit messages or amended drafts wipe the reference.
- **Every merge to `main` rebases the SHAs of the commits it ships.** Other open PRs were based on `main`'s tip at the moment they were opened. After a rebase merge, those base SHAs may no longer be reachable in `main`'s first-parent line. GitHub's diff view reports the open PRs as "N commits behind" with phantom commits, and the only way to clean it up is for the PR author to rebase. With several active PRs and an active merge cadence, the rebase queue is constant — review-grade diffs evaporate before reviewers can read them.
- **Surgical revert is gone for the same reason as squash:** the SHA on `main` is not the SHA the author committed; rewrites mean revert targeting a "known" SHA is brittle.

### Merge commit

`Create a merge commit` (the default) creates a single new commit on `main` whose first parent is the previous tip of `main` and whose second parent is the PR branch's tip. The PR branch's individual commits remain on disk, reachable through the second parent.

- **PR boundary is the merge commit itself.** Two parents == unmistakable, regardless of message text. `git log --merges --first-parent` enumerates every PR even if every merge message is blank.
- **Surgical revert is one command:** `git revert -m 1 <merge-sha>` reverts an entire PR atomically.
- **Bisect descends into the second parent** when it needs per-commit blame inside the PR. Granularity stays available.
- **Open PRs aren't churned.** A merge to `main` adds one new commit at the tip; existing open PRs' base commits remain valid until the author chooses to update.

The cost is that the linear-history view (`git log` without `--first-parent`) is busier — branch commits show up alongside the merge commits — and the working `main` carries more total commits. That trade is accepted; the audit and recovery surface above is worth it.

### `--admin` is not the loophole

The repository's branch protection has both **squash** and **rebase** disabled. `gh pr merge --admin` will bypass that check. `--admin` exists for skipping CI on doc-only PRs (when explicitly authorized) and similar narrow cases — it is **not** the way to get around the merge-method rule. A `--admin` merge with `--squash` or `--rebase` is a violation, not a permitted exception.

## Decision

Every PR merges with **merge commit only**. The accepted invocations:

```sh
gh pr merge <num> --merge --delete-branch
gh pr merge <num> --admin --merge --delete-branch    # --admin only when CI-skip is otherwise authorized
```

Forbidden, without exception:

- `gh pr merge ... --squash` / `-s`
- `gh pr merge ... --rebase` / `-r`
- `git merge --squash` of a PR branch into `main` followed by a manual push
- Any other path that lands a PR's contents on `main` without a merge commit whose second parent is the PR branch tip

Rationale composes with [ADR-0007 (pit of success)](0007-pit-of-success.md): the merge-method rule is enforced structurally where possible — branch protection has squash and rebase off; an agent-side hook denies `gh pr merge` invocations carrying `--squash`/`-s`/`--rebase`/`-r` before the command reaches `gh`. The rule is in force whether or not those gates are in place; the gates exist so violations cost zero attention to catch.

## Consequences

- **`git log --merges --first-parent main` is the canonical PR ledger.** Every entry is a PR boundary; the count is the PR count.
- **`git revert -m 1 <merge-sha>` is the canonical "back out a PR" operation.** It works regardless of the PR's commit count or merge-message contents.
- **`git bisect` retains per-commit resolution inside a PR** by descending into the second parent when narrowing.
- **Open PRs are stable across merges to `main`.** A PR opened against `main@X` stays based on `X` until the author rebases or merges `main` in. No churn from unrelated merges.
- **`main`'s linear log is busier.** Reviewers reading `git log` without `--first-parent` see branch commits interleaved with merge commits. Tooling that wants the PR-only view uses `--first-parent`. Accepted trade.
- **Composes with the no-direct-pushes-to-`main` rule.** Every change on `main` is a merge commit by construction — `git log --first-parent` doubles as the change-control audit trail.
- **The hard `--admin` discipline.** `--admin` is for CI bypass only and only when otherwise authorized. It does not authorize squash or rebase. The agent-side hook denies the offending flags regardless of `--admin`.
