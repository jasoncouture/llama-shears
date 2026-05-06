#!/usr/bin/env bash
# Pre-push guard: rebuild from a clean slate and refuse to push if the
# build regenerated docs/api/ in a way that doesn't match what's committed.

set -euo pipefail

echo "=== Cleaning build artifacts"
dotnet clean --nologo -v q

echo "=== Building"
dotnet build --nologo -v q

echo "=== Checking for changes"
dirty="$(git status --porcelain -- docs/api)"

if [ -n "$dirty" ]; then
  cat >&2 <<'MSG'

============================================================
PUSH ABORTED — docs/api/ is out of date.

A clean rebuild regenerated content under docs/api/ that does not
match what is committed. The remote would land in a state it cannot
itself reproduce from source. Commit the regenerated docs first:

    git add docs/api
    git commit -m "docs(api): regenerate"
    git push

If you genuinely need to bypass this (you almost certainly don't),
push with --no-verify and own it.
============================================================
MSG
  echo "$dirty" >&2
  exit 1
fi

echo "=== Documentation is up to date"
