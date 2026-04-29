# AGENTS.md

## Agent Instructions

Agents must:

1. Read the memory index files, if they exist, in both:
	- agents/memories/local/INDEX.md
	- agents/memories/shared/INDEX.md
2. Use [Conventional Commits](https://www.conventionalcommits.org/) for all commit messages.
3. Make atomic commits—each commit should represent a single, self-contained change.

For further documentation, see the memory index files in the respective directories.

## Memory Storage Guidelines

- Project-wide instructions belong in the shared memories directory: `agents/memories/shared/`
- User-specific/local instructions belong in the local memories directory: `agents/memories/local/`
- If it is unclear which category a memory belongs to, ask the user for clarification before saving.

## C# Guidelines

- Do not use primary constructors on regular classes or structs. Only records may use primary constructors. Disable the associated IDE analysis warnings.
- Use dotnet CLI/tools to create projects, solutions, add packages, etc. Do not generate these items by hand.
- Test framework: TUnit.
- All code must have tests unless it is too difficult or cumbersome to do so.
