# ADR-0005: One top-level type per file

## Status

Accepted (2026-04-27). Enforced by `LS0005` (`OneTypePerFileAnalyzer`), with code fix `OneTypePerFileCodeFixProvider`.

## Context

C# permits any number of top-level type declarations in a single file. The compiler does not care; the type lookup, the namespace structure, and the build are all driven by the *declaration*, not the file boundary. The file system is, from the compiler's perspective, a packaging detail.

Humans and tooling are not as flexible:

- **Navigation.** "Go to definition" lands on the type. "Open file" requires guessing which file the type lives in. Convention — file name matches type name — only holds if there is one type per file.
- **Diff and blame.** A change to type `Foo` and a change to type `Bar` collapse into the same file's history when they share a file. Per-type history requires per-type files.
- **Mental anchoring.** A file is the unit a reader holds in their head while editing. One type per file makes that unit coherent: opening the file tells you exactly what is there.
- **Refactoring.** Moving a type between projects, namespaces, or assemblies is trivial when the type owns its file. It is fiddly when the type shares one.

The cost of the rule is purely mechanical: more files, longer directory listings. Modern IDEs and file systems handle that without complaint.

## Decision

Each C# source file declares at most one top-level type. Top-level types are classes, structs, interfaces, enums, records, record structs, and delegates declared directly under the compilation unit or under a (block- or file-scoped) namespace. Nested types are unaffected — they belong to their outer type's declaration and travel with it.

The rule is non-configurable; it is enforced as a hard compile error by `LS0005`. The companion code fix extracts the offending type into a sibling document named `{TypeName}.cs`, preserving the original file's `using` directives and namespace structure. Running the fix once per extra type splits the file completely.

## Consequences

- Every type has a predictable home: `TypeName.cs`. Tooling that maps file name to type name works reliably.
- File counts go up. A type that used to share a file now has its own.
- Generated code that emits multiple types into one file (some serializer scaffolding, source generators that don't follow the convention) trips the rule. Generated outputs that we ship are expected to follow the same convention or to live outside the analyzer-applied folder structure.
- Trivial helpers — small enums, single-method delegate types, sentinel records — that previously lived alongside their primary consumer now require their own file. The cost is real but constant.
