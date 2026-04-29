# ADR-0012: XML doc comments default to absent

Accepted 2026-04-28.

## Context

XML doc comments are reflexively attached to every public symbol in many .NET codebases. The IDE prompts for them, analyzer templates expect them, and developers carry the habit forward. The result is documentation that:

- Restates what the name already says.
- Drifts from reality as code evolves and the comment doesn't.
- Mixes audiences — developer-facing contract notes intermixed with user-facing operational detail.
- Trains readers to skip every doc block, including the rare one that earns its place.

A doc block that does not tell the reader something the name doesn't is a cost — reading time, review time, drift risk — without the corresponding benefit.

User-facing operational details and developer-facing contract details are different things for different audiences. They belong on different surfaces. A developer opening `IShearsPaths.GetPath` in the IDE wants to know what to *use* the symbol for. An operator running the host wants to know where data actually goes and how to relocate it. Cramming both into one XML block serves neither audience well, drifts twice as fast, and bloats the file in the editor.

## Decision

XML doc comments default to absent. They are added only where they earn it.

**Mandatory:**

- Public interface members. The interface is the contract; the comment documents it for callers and implementers.

**Discouraged:**

- Public symbols whose name and signature describe their meaning fully. `DataRootEnvironmentVariableName` does not need a comment.
- Private methods, properties, and fields. Internal documentation lives in naming and structure, not XML.
- Implementations of interface members. Use `<inheritdoc/>` instead of restating.

**Process:**

- If a symbol seems to need a comment, fix the name first. The comment is the second-best answer.
- XML comments are developer-context: what to *use* the symbol for, what callers can rely on. Operational details (default values, environment-variable overrides, file paths, runtime behaviour an operator cares about) belong in user-facing docs (README, ADRs, design docs), not in XML.
- When in doubt, leave the doc out. A reader can read the signature; they cannot un-read restated obviousness.

## Consequences

- The bar for adding an XML doc is "what does this comment tell the reader that the signature doesn't?" If the answer is nothing, the comment is not added.
- Code review treats XML docs the same as code: dead weight gets removed.
- Public interfaces remain fully documented — that is the contract.
- Implementations stay terse via `<inheritdoc/>`.
- User-facing operational details live in one canonical place — the README or an ADR — rather than scattered across files.
- Existing doc blocks that restate obvious names or describe operator concerns from a developer audience get pruned as their files are touched, not in a one-shot sweep.

This ADR pairs with [ADR-0009 (Pragmatism over technical purity)](0009-pragmatism-over-technical-purity.md): "every public symbol must have a doc comment" is exactly the consistency-for-consistency's-sake mandate ADR-0009 calls out. Targeted documentation of contracts and non-obvious surface is more truthful than uniform documentation of everything; the project explicitly chooses the former.
