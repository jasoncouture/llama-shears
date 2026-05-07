#!/usr/bin/env bash
# Decide whether the current container build earns the :latest tag.
#
# Writes `tag-latest=true` or `tag-latest=false` to $GITHUB_OUTPUT.
#
# Rule: only release/** pushes ever earn :latest. main pushes are
# naturally pre-release per NBGV; PR builds don't push at all. On a
# release/** push, the build earns :latest only when its NBGV
# SemVer2 (passed via THIS_VER) is the highest stable (no
# pre-release suffix) tag observable on the GHCR package, plus
# itself. The very first publish of a package qualifies.
#
# Required environment variables:
#   GH_TOKEN     - token with read access to the package's versions
#   OWNER        - GHCR owner (lowercase)
#   PACKAGE      - URL-encoded package path, e.g. <repo>%2F<service>
#   THIS_VER     - sanitised SemVer2 of the current build
#   EVENT_NAME   - github.event_name
#   REF          - github.ref
#   GITHUB_OUTPUT - GHA step output file (provided by the runner)

set -euo pipefail

flag=false

if [ "${EVENT_NAME}" = "push" ] && [[ "${REF}" == refs/heads/release/* ]]; then
    existing=$(gh api -X GET "/users/${OWNER}/packages/container/${PACKAGE}/versions" --paginate 2>/dev/null \
               | jq -r '.[].metadata.container.tags[]?' \
               || true)
    top=$(printf '%s\n%s\n' "${existing}" "${THIS_VER}" \
          | grep -E '^[0-9]+\.[0-9]+\.[0-9]+$' \
          | sort -V \
          | tail -1 || true)
    if [ "${top}" = "${THIS_VER}" ]; then
        flag=true
    fi
fi

echo "tag-latest=${flag}" >> "${GITHUB_OUTPUT}"
