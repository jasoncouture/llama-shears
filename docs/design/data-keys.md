# Typed data keys

The data-context scope (`IDataContextScope`) is the carrier for everything a Scriban template, an MCP tool, or an inference middleware step might want to read: the active `AgentConfig`, `ModelConfiguration`, `WorkspaceContext`, wall-clock, todo list, retrieved memories, channel id, and so on. Today every consumer agrees on a `const string DataKey = "..."` declared on the value type, and the dict is `Dictionary<string, object?>`. That works, but it has three problems:

1. The key string and the value type are two pieces of paperwork that have to stay in sync by convention. Nothing in the compiler tells you the value at `"agent_configuration"` is actually `AgentConfig`.
2. There is no single place to look up "what keys exist and what's in them," so the prompt-context surface drifts away from the template every time someone adds a key.
3. There is no enforcement that a new key gets documented before it ships.

The plan: a small typed wrapper, an analyzer that requires a doc summary, and a build-time generator that walks every `DataKey<T>` declaration and emits a markdown reference.

## Shape

```csharp
public abstract class DataKey : IEquatable<DataKey>
{
    public string Name { get; }
    protected DataKey(string name) => Name = name;

    public static implicit operator string(DataKey key) => key.Name;

    public bool Equals(DataKey? other) => other is not null && Name == other.Name;
    public override bool Equals(object? obj) => obj is DataKey other && Equals(other);
    public override int GetHashCode() => Name.GetHashCode(StringComparison.Ordinal);
    public override string ToString() => Name;
}

public sealed class DataKey<T> : DataKey
{
    public Type ValueType => typeof(T);

    public DataKey(string? name = null)
        : base(name ?? typeof(T).Name)
    {
    }
}
```

The non-generic base is what the dictionary actually keys on (`Dictionary<DataKey, object?>` or any other store that wants a stable identity without caring about the value type). The generic subclass carries `T` so any read-site can be typed: `scope.Get(keys.AgentConfig)` returns `AgentConfig`, no out-parameter dance.

The implicit `→ string` keeps the migration cheap: anything that still wants the string (existing dictionaries, Scriban globals, JSON wire format) reads it the same way it always has.

### Default name

If the caller doesn't pass a string, the key falls back to `nameof(T)`. Two consequences:

- New keys without an explicit name use a PascalCase string that matches the C# type. Scriban exposes those verbatim (and Scriban already lower-snake-cases property accesses on records, so `{{ agent_configuration.id }}` doesn't break — only the *registry key* differs).
- Existing keys (`"agent_configuration"`, `"model_configuration"`, `"workspace"`, `"todo_list"`, …) are different strings from `nameof(T)`. They stay as explicit names during migration so templates keep rendering without churn.

### Equality

Two `DataKey<T>` instances with the same `Name` are equal. The dict treats them as the same slot. Hashing uses `StringComparer.Ordinal` — the names are stable identifiers, not user input.

## One-line summary lives on the field, doc table links to T

Every `DataKey<T>` field is a `public static readonly` declaration with an XML `<summary>` comment. The summary is a *single line* — it's the cell text in the generated reference table, nothing more. The full explanation of the value lives on `T` itself (its class docs, its property docs), which the table links to.

```csharp
/// <summary>The active agent configuration for the current scope.</summary>
public static readonly DataKey<AgentConfig> AgentConfig = new("agent_configuration");

/// <summary>Workspace path plus the bootstrap files the system prompt template renders.</summary>
public static readonly DataKey<WorkspaceContext> Workspace = new();   // key defaults to "WorkspaceContext"
```

A new analyzer (provisional `LS00xx`) raises an error when a `DataKey<T>` field is missing `<summary>`. Bare minimum: a sentence. Anything richer goes on `T`'s own XML doc, where the API-docs generator will already pick it up.

### Constructor argument must be compile-time-known

A sibling analyzer hooks `ObjectCreationExpressionSyntax`, narrows to instantiations of `DataKey<T>`, and inspects the argument list:

- **No argument** — accept. The default `nameof(T)` path applies.
- **One argument** — call `SemanticModel.GetConstantValue(argExpression)`. If `HasValue` is true, accept; otherwise emit the diagnostic. `GetConstantValue` collapses literal strings, `const string` field references, and `nameof(...)` expressions to a compile-time value, so all three forms pass without separate handling.
- **Two or more arguments** — the ctor signature catches this before the analyzer sees it.

The intent: a key's wire string is part of the source code, not assembled at runtime. The docs-gen scanner can read it from the syntax tree; templates and other consumers see a stable identifier; reviewers never have to wonder which path computes which key.

### Why not a dedicated attribute?

Two channels for the same prose drift apart. ADR-0012 already mandates XML doc on public interface members; the field declaration is exactly that. IntelliSense surfaces the summary at every use site without a custom attribute reader. The docs-build pipeline already parses XML doc comments. Reuse what's there.

## Generated reference table

A build-time target (extension of `LlamaShears.DocsBuild`) reflects the assembly, finds every `public static readonly DataKey<T>` field, and writes a markdown section like:

```markdown
## Template data

- [agent_configuration](../api/.../AgentConfig.md) — The active agent configuration for the current scope.
- [workspace](../api/.../WorkspaceContext.md) — Workspace path plus the bootstrap files the system prompt template renders.
- [todo_list](../api/.../TodoItem.md) — Snapshot of the agent's todo list.
- …
```

Specifics:

- **Row label** is the scriban name (`key.Name`).
- **Link target** is the class docs page of `T`, not `DataKey<T>` itself. `DataKey<T>` is documented on its own once; the table is about *what value sits behind each key*.
- **Row text** is the one-line `<summary>` of the field.
- The table is sorted by name so a code diff against the doc never reorders.

## Placement of the registry

Two reasonable layouts:

1. **Per-type ownership** — every value type carries its own key as a static member (e.g. `AgentConfig.Key`). The "abstractions own their keys" pattern. Doc-gen has to walk every assembly.
2. **Central registry** — a single `DataKeys` static class lists every key. One file to read, one file to scan. Easier doc-gen, but the key declaration is decoupled from its value type.

Lean toward central. Picking it is the first decision tomorrow; the rest of the design works either way.

## Migration outline

1. Land the `DataKey` / `DataKey<T>` types in `LlamaShears.Core.Abstractions.Common`.
2. Add the analyzer rule for `<summary>` enforcement.
3. Convert one well-known key (`AgentConfig.DataKey`) end-to-end as the canary — declaration, consumers, JSON serialization (none — keys never round-trip to disk).
4. Sweep the rest of the keys.
5. Wire the docs-build extension to emit the table; thread it into the existing API-docs index.

`IDataContextScope` keeps its current `string`-keyed API during the sweep (because of the implicit conversion); once everything is migrated, the storage backing dictionary can switch to `Dictionary<DataKey, object?>` and typed accessors replace the `TryGetValue<T>(string, out _)` paths.

## Live counterpart: data explorer

The generated reference describes the keys *statically*. A per-agent **data explorer** in the Web UI gives the *runtime* view: walk the live scope, render each entry as a table row of `key` → pretty-printed JSON. When `JsonSerializer.Serialize` chokes on a value (cycles, unmappable types, recalcitrant converters), fall back to `value?.ToString() ?? "null"` so one bad entry doesn't blank the page. The static doc and the live explorer share the same key vocabulary; together they answer "what's supposed to be in the scope" and "what's actually in the scope right now" in one place each.
