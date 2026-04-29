# Contributing

Thanks for working on LlamaShears. This document captures the conventions and expectations for changes to the codebase. Most of them are encoded in tooling — analyzers, the build, the editor config — so following them mostly means letting the tools do their job and not fighting them. The parts that aren't tool-enforced are listed here.

The guiding principle behind everything below is [ADR-0007 — Pit of success](docs/adr/0007-pit-of-success.md): the easy path should be the correct path, and doing the wrong thing should be inconvenient enough that the inconvenience itself is a signal.

## Repository layout

| Path             | What lives there                                                                 |
|------------------|----------------------------------------------------------------------------------|
| `src/`           | Production code. Every project here picks up the analyzer ruleset automatically. |
| `tests/`         | Cross-project unit tests (TUnit).                                                |
| `analyzers/`     | The local Roslyn analyzer assembly, its code-fix sibling, and analyzer tests.    |
| `docs/`          | Project documentation. See `docs/INDEX.md`.                                      |
| `docs/adr/`      | Architectural Decision Records. See `docs/adr/INDEX.md`.                         |
| `docs/design/`   | Design notes for in-flight or recently-landed subsystems.                        |
| `agents/`        | Agent-facing instructions and shared/local memory for AI collaborators.          |

## Building and running

```sh
dotnet build
dotnet test
```

Tests run via Microsoft Testing Platform; do not pass `--filter` or scope to a specific class. **Always run the entire test suite** before committing — a partial run is not a verification.

The `dotnet-ef` tool is pinned via `dotnet-tools.json`; restore it once with `dotnet tool restore`.

## Code style

Almost every style rule is a hard compile error enforced by the local analyzers in [analyzers/LlamaShears.Analyzers/](analyzers/LlamaShears.Analyzers/). The complete set of accepted decisions, with rationale, lives in [docs/adr/INDEX.md](docs/adr/INDEX.md). Highlights:

- **No primary constructors on non-record types** — [ADR-0004](docs/adr/0004-no-primary-constructors-on-non-record-types.md), `LS0001`. Records only.
- **No public or internal fields** — [ADR-0002](docs/adr/0002-no-public-fields.md), `LS0002`. Use a property. `const` is exempt.
- **Field names start with `_`** — [ADR-0003](docs/adr/0003-underscore-prefix-for-fields.md), `LS0003`. `const` is exempt; `static readonly` is not.
- **No `this.` qualifier** — [ADR-0001](docs/adr/0001-no-this-qualifier.md), `LS0004`. Required for extension-method calls is the only carve-out, and that case raises `LS0006` as a smell warning ([ADR-0006](docs/adr/0006-extension-method-on-this-is-a-smell.md)).
- **One top-level type per file** — [ADR-0005](docs/adr/0005-one-type-per-file.md), `LS0005`. Code fix extracts extras to sibling files.

A few additional conventions that are not analyzer-enforced:

- **Collection expressions** — Prefer `[a, b]` over `ImmutableArray.Create`, `new List<>()`, `new[] { ... }`, etc. Builder/list patterns that compose at runtime are the exception.
- **Options binding** — Never accept `IConfiguration` as a parameter. Use `services.AddOptions<T>().BindConfiguration("Section")` and expose the section name (defaulted) as the only configurable surface on the registration extension.
- **EF entities** — No navigation properties. Relationship configuration lives on the principal (referenced) side. No logic in property setters; validation belongs in `ISaveChangesHook` implementations.
- **Hooks order** — `ISaveChangesHook` execution order is undefined and must not be relied on. Order-dependent work belongs combined into a single hook.

## Commit conventions

- [Conventional Commits](https://www.conventionalcommits.org/) for every commit message. The first line carries type/scope/summary; further detail goes in the body.
- **Atomic commits.** Each commit is one self-contained change. A bug fix is a commit. The refactor that enables the fix is a separate commit. The test for the fix is part of the fix commit, not a follow-up.
- **Every commit must build.** `dotnet build` and `dotnet test` (the entire suite) must succeed at every commit unless the commit message explicitly notes otherwise.
- Pre-commit hooks are not skipped. If a hook fails, fix the underlying issue and create a *new* commit; do not amend after a hook failure (the failed commit never existed).
- Never `git push` without explicit go-ahead. Local commits are free; pushes are deliberate.

## Pull requests

If a change is non-trivial:

- The PR description explains the *why*, not just the *what*. The diff already shows the what.
- Cross-reference any relevant ADRs or design docs.
- Verify both `dotnet build` and `dotnet test` are green locally before opening the PR.

## Adding a new ADR

1. Pick the next sequential number after the last entry in [docs/adr/INDEX.md](docs/adr/INDEX.md).
2. Create `docs/adr/####-short-kebab-title.md`. Title line: `# ADR-####: <Title>`. Add an `Accepted YYYY-MM-DD.` line directly under the title (and an `Enforced by ...` clause if there is an analyzer).
3. Sections: `Context`, `Decision`, `Consequences` — Michael Nygard format.
4. Add the entry to the index. Trail with `— Analysis ID LS####` if there is an analyzer; omit the trailer otherwise.
5. ADRs are immutable once accepted. Future changes go in a new ADR that supersedes the old one.

## Adding a new analyzer

1. Diagnostic ID and category live in [`Diagnostics/DiagnosticIds.cs`](analyzers/LlamaShears.Analyzers/Diagnostics/DiagnosticIds.cs) and [`Diagnostics/DiagnosticCategories.cs`](analyzers/LlamaShears.Analyzers/Diagnostics/DiagnosticCategories.cs).
2. Descriptor lives in [`Diagnostics/Descriptors.cs`](analyzers/LlamaShears.Analyzers/Diagnostics/Descriptors.cs). Hard rules carry `WellKnownDiagnosticTags.NotConfigurable`; smells/warnings do not.
3. Analyzer class lives next to its peers in [analyzers/LlamaShears.Analyzers/](analyzers/LlamaShears.Analyzers/). Code fixes (if any) live in [analyzers/LlamaShears.Analyzers.CodeFixes/](analyzers/LlamaShears.Analyzers.CodeFixes/) — they cannot share an assembly with the analyzer (RS1038).
4. Tests live in [analyzers/LlamaShears.Analyzers.Tests/](analyzers/LlamaShears.Analyzers.Tests/) and use the in-process `AnalyzerHarness` / `CodeFixHarness`.
5. Add the rule to [`AnalyzerReleases.Unshipped.md`](analyzers/LlamaShears.Analyzers/AnalyzerReleases.Unshipped.md).
6. Write the corresponding ADR.
