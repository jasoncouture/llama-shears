## Coding and Commit Requirements

- Only one type (class, interface, enum, record, etc.) per file. Do NOT pack multiple types in a single file.
- All commits must build successfully.
- All tests must pass before commit, unless the commit message explicitly notes otherwise.
- Always run `dotnet test` (all tests, not just a specific project) before committing.
## Proactive Memory and Instruction Updates

- Agents must proactively update local memories, shared memories, and AGENTS.md with relevant workflow, architecture, and process notes as decisions or instructions are made.
- If a decision or instruction is ambiguous, ask the user for clarification before saving.
## .NET Solution Format

This project uses a modern .NET SDK. The `.slnx` file is the default solution format and should be preferred by agents for all solution-related operations.
# AGENTS.md

## Agent Instructions

Agents must:

1. Read **only** the memory index files, if they exist, to learn what memories are available:
	- [memories/local/INDEX.md](memories/local/INDEX.md)
	- [memories/shared/INDEX.md](memories/shared/INDEX.md)

   Do **not** read the individual memory files up front. Pull a specific memory on demand when its index entry indicates it is relevant to the current task.
2. Use [Conventional Commits](https://www.conventionalcommits.org/) for all commit messages.
3. Make atomic commits—each commit should represent a single, self-contained change.

Memory index files are to be read at the start of any fresh context.

## Memory Storage Guidelines

> **Note:** All paths in this section (and in the Agent Instructions section above) are **relative to this `agents/` folder**, not the repository root. The actual on-disk locations are `agents/memories/shared/` and `agents/memories/local/`.

- Project-wide instructions belong in the shared memories directory: [memories/shared/](memories/shared/)
- User-specific/local instructions belong in the local memories directory: [memories/local/](memories/local/)
- If it is unclear which category a memory belongs to, ask the user for clarification before saving.

## C# Guidelines

- Do not use primary constructors on regular classes or structs. Only records may use primary constructors. Disable the associated IDE analysis warnings.
- Use dotnet CLI/tools to create projects, solutions, add packages, etc. Do not generate these items by hand.
- Test framework: TUnit.
- All code must have tests unless it is too difficult or cumbersome to do so.
