# Data Layer

The `LlamaShears.Data` project owns all persisted state for the host. It targets SQLite via Entity Framework Core. The project intentionally references no other project in the solution: data abstractions, entities, the `DbContext`, interceptors, and DI registration all live here, and consumers (currently `LlamaShears.Api.Host`) reference this project — never the other way around.

## Object Contracts

Three independent marker interfaces. Entities pick the ones that apply; they are not a hierarchy.

- `IDataObject` — `Guid Id { get; init; }`. For entities with a single-column identity. Composite-key entities (e.g. join tables) deliberately do not implement this.
- `ICreated` — `DateTimeOffset Created { get; init; }`. Set unconditionally on add by the timestamp interceptor and immutable thereafter.
- `ILastModified` — `DateTimeOffset LastModified { get; set; }`. Set unconditionally on add and on update by the timestamp interceptor.

The split exists because not every persisted row needs all three. A join row typically has a composite key (no `Id`) and a `Created` for ordering, but no `LastModified` (rows are append-only). Forcing such a row through a "DataObject" hierarchy would require fields that do not belong on it.

Ids are UUIDv7. Callers may set `Id` explicitly when constructing an entity; if left as `Guid.Empty`, the data layer assigns one.

### No navigation properties

EF entities expose only scalar properties and foreign-key columns. Reference and collection navigation properties are deliberately not declared. Related lookups are performed explicitly by DTO mappers / query layers using the foreign key.

The reason is operational: navigation properties make it easy to silently introduce eager loading, lazy loading, or N+1 query patterns whose cost is invisible at the call site. Forcing related fetches to go through an explicit query keeps the data access pattern visible in code review.

Concretely: `SessionMessage.SessionId` exists, but neither `SessionMessage.Session` nor `Session.Messages` does.

### Relationship configuration lives on the principal (referenced) side

Relationships are configured in the `ConfigureModel` of the *referenced* entity, not the referencing one. For Session ← SessionMessage, the configuration

```csharp
entity.HasMany<SessionMessage>()
    .WithOne()
    .HasForeignKey(m => m.SessionId)
    .OnDelete(DeleteBehavior.Cascade);
```

lives in `Session.ConfigureModel`. `SessionMessage` only declares its own scalars and indexes.

The reasoning: the dependent side already advertises the relationship through its FK column (`SessionId` is self-documenting). The principal side, in contrast, has no other signal that anything points at it. Putting the relationship config on the principal gives a one-stop "who references me" view per entity, which is exactly the visibility navigation properties would have provided — minus the runtime hazards.

## Save-Change Hooks

A single EF Core `SaveChangesInterceptor` (`SaveChangesHookInterceptor`) is attached to the pooled context. It does no domain work itself; on each `SaveChanges`, it makes one forward pass over the change tracker and invokes every registered `ISaveChangesHook` against each tracked entity in the Added, Modified, or Deleted state.

```csharp
public interface ISaveChangesHook
{
    void Apply(EntityEntry entry, SaveChangesHookContext context);
}
```

`SaveChangesHookContext` is constructed once per save and shared with every hook invocation. It carries:

- the `DbContext` the save is running against, and
- a single `UtcNow` snapshot, so all timestamps written within one save are consistent regardless of which hook wrote them.

### Why a single interceptor with hooks

- **One pass.** Two per-concern interceptors meant two iterations over the change tracker per save. With hooks, the change tracker is iterated once and each entity is shown to every hook in turn.
- **Pluggable behaviors.** New per-entity behaviors (audit trails, soft-delete bookkeeping, validation) become a new `ISaveChangesHook` implementation registered in DI; no new EF wiring.
- **Order is undefined.** Hook execution order is intentionally not specified, not guaranteed, and not stable across versions. Hooks must be designed to operate on disjoint slices of entity state; if two pieces of work need a defined sequence, they belong in a single hook. Relying on observed registration order is a bug. If a hook throws, the save is aborted.

### Registered hooks

Each registered hook handles **one** marker interface. This is the order-independence rule applied to ourselves: splitting per-interface is the easiest way to guarantee the hooks have nothing to coordinate.

#### `IdGenerationHook` — `IDataObject.Id`
- **Add:** if `Id == Guid.Empty`, assign `Guid.CreateVersion7()`. Otherwise leave the caller's value untouched.
- **Update:** throw `InvalidOperationException` if `Id == Guid.Empty` *or* if the property has been marked modified since materialization. Ids are immutable after creation.
- **Delete:** no-op.

#### `CreatedHook` — `ICreated.Created`
- **Add:** unconditionally set `Created` to `context.UtcNow`.
- **Update:** throw `InvalidOperationException` if `Created` has been modified.
- **Delete:** no-op.

#### `LastModifiedHook` — `ILastModified.LastModified`
- **Add or update:** unconditionally set `LastModified` to `context.UtcNow`.
- **Delete:** no-op.

#### `AgentNameHook` — `Agent.Name` format / immutability
Per-entity hook for `Agent`. Validates the name format against `^[a-z][a-z0-9]*$` and throws if `Name` is modified on update. Does not rewrite the caller's input — invalid values throw rather than being silently lowered. See "Agent.Name" above for the full rationale.

The "throw on illegal mutation" stance is deliberate: silent reverts hide caller bugs; throwing surfaces them at save time.

Hooks are registered via `services.AddDatabaseHook<T>()`, which calls `TryAddEnumerable` so hooks accumulate cleanly without duplicates and a re-registered hook is not added twice. The interceptor receives the full set as `IEnumerable<ISaveChangesHook>` from DI. Each hook touches a single property family on a single interface (or a single entity type, in the case of `AgentNameHook`), so order is irrelevant by construction.

## Per-Entity Model Configuration

EF Core's idiomatic options are (a) a monolithic `OnModelCreating` body or (b) `IEntityTypeConfiguration<T>` classes registered via `ApplyConfigurationsFromAssembly`. We use neither.

Instead, each entity implements:

```csharp
public interface IModelConfigurable<TSelf>
    where TSelf : class, IModelConfigurable<TSelf>
{
    static abstract void ConfigureModel(EntityTypeBuilder<TSelf> entity);
}
```

`LlamaShearsDbContext.OnModelCreating` calls `modelBuilder.ConfigureAllModels()`, which in turn calls a thin generic dispatcher per entity:

```csharp
public static ModelBuilder ConfigureModel<T>(this ModelBuilder modelBuilder)
    where T : class, IModelConfigurable<T>
{
    var entityBuilder = modelBuilder.Entity<T>();

    if (typeof(T).IsAssignableTo(typeof(IDataObject)))
    {
        entityBuilder.HasKey(nameof(IDataObject.Id));
        entityBuilder.Property(nameof(IDataObject.Id)).ValueGeneratedNever();
    }

    if (typeof(T).IsAssignableTo(typeof(ICreated)))
    {
        entityBuilder.Property(nameof(ICreated.Created)).IsRequired();
    }

    if (typeof(T).IsAssignableTo(typeof(ILastModified)))
    {
        entityBuilder.Property(nameof(ILastModified.LastModified)).IsRequired();
    }

    T.ConfigureModel(entityBuilder);
    return modelBuilder;
}
```

The dispatcher applies the conventions implied by the data interfaces *before* invoking the entity's own `ConfigureModel`, so:

- Entities never repeat the `Id` / `Created` / `LastModified` boilerplate.
- An entity may still override or extend any of it from its own `ConfigureModel` (called last).
- Adding a new sibling marker interface (e.g. `ISoftDeletable`) is a single new parallel `if` in the dispatcher.

The three type checks are independent and parallel; an entity that implements only some of the interfaces gets only the relevant conventions. Composite-key join entities skip the `IDataObject` block entirely and declare their key in their own `ConfigureModel`.

### Why this pattern

- **Explicit ownership.** The `DbContext` lists every entity it owns; nothing is registered by reflection or assembly scanning. The set of mapped entities is statically obvious from one place.
- **Co-located mapping.** An entity's storage shape lives next to its declaration, not in a sidecar config class.
- **Compile-time enforcement.** `static abstract` means the compiler refuses entities that forget to provide a mapping.
- **Scoped surface.** Entities receive an `EntityTypeBuilder<TSelf>`, not the whole `ModelBuilder`. This makes it harder to accidentally configure another entity's mapping from the wrong place. Bidirectional EF concerns (e.g. `HasMany().WithOne()` configuring the inverse navigation) still work — they're a property of EF, not of this pattern — but the convention is "configure the relationship from one side only," and the narrowed builder reinforces it.

The trade-off vs. the standard EF idiom is that we step off the well-trodden path. This is a small, deliberate cost.

## Entities

| Entity          | Interfaces                                  | Notes                                                                                                |
|-----------------|---------------------------------------------|------------------------------------------------------------------------------------------------------|
| `Agent`         | `IDataObject`, `ICreated`, `ILastModified`  | Persistent identity for an agent: `Id`, timestamps, and a unique immutable `Name`. Configuration (model, parameters, on-disk path) is **not** stored here — that is a separate, deferred design decision; the file/disk vs. DB question is intentionally open. |
| `Session`       | `IDataObject`, `ICreated`, `ILastModified`  | LLM conversation header. Owns inbound relationships from `SessionMessage` and `AgentSession`.        |
| `SessionMessage`| `IDataObject`, `ICreated`                   | Single LLM context entry. Append-only — no `LastModified`.                                           |
| `AgentSession`  | `ICreated`                                  | Join row linking `Agent` ↔ `Session`. Composite PK `(AgentId, SessionId)`; unique index on `SessionId` alone. |

### `Agent.Name`

`Name` is `required` and `init`-only on the entity, so it is set exactly once at construction and never afterwards. The property itself carries **no logic** — by deliberate choice, validation is not in the setter. It lives in `AgentNameHook`, a save-changes hook, so the entity stays a plain data shape and all save-time invariants are enforced through the same mechanism.

The hook enforces:

- **Format:** the value must match `^[a-z][a-z0-9]*$` exactly — a single lowercase ASCII letter followed by zero or more lowercase ASCII letters or digits. Non-canonical input (including any uppercase letter) throws `InvalidOperationException` with the offending value attached via `Exception.Data["agentName"]`.
- **No silent normalization:** the hook does **not** rewrite the caller's input. If a caller submits `FooBar`, the save fails. Normalizing to canonical form is the caller's responsibility — same stance as Id and Created. This keeps the persisted value byte-equal to what the caller submitted, so there is no hidden mutation between intent and storage.
- **Immutability:** on update, throw if the `Name` property has been marked modified. (`init`-only blocks this from C# code already; the hook is defense-in-depth against direct change-tracker manipulation.)

The DB enforces uniqueness via `entity.HasIndex(a => a.Name).IsUnique()`. Because the hook rejects mixed case before save, the DB never sees two values that differ only in case.

### `AgentSession` — many-to-many with constraints

`AgentSession` is the agent ↔ session ownership ledger. It is also the active-session history: the *current* session for an agent is the row with the most recent `Created` for that `AgentId`. Older rows are not deleted; they record prior sessions.

- **Composite PK:** `(AgentId, SessionId)`. No surrogate `Id`.
- **Unique index on `SessionId`:** any given session belongs to at most one agent. The combination of composite PK + standalone unique index is portable across SQLite, PostgreSQL, SQL Server, etc.
- **Append-only:** no `LastModified`. New rows are inserted; existing rows are never updated. Deletion happens only via cascade from the principal sides.
- **Cascade behavior:** deleting an `Agent` cascades to its `AgentSession` rows; deleting a `Session` cascades to the `AgentSession` row that points at it. The `Session` itself is not deleted when its `Agent` is — the conversation rows live on, simply without an owning agent.

The relationship config follows the principal-side rule: `Agent.ConfigureModel` declares `HasMany<AgentSession>().WithOne().HasForeignKey(x => x.AgentId)`, and `Session.ConfigureModel` declares `HasMany<AgentSession>().WithOne().HasForeignKey(x => x.SessionId)`. `AgentSession.ConfigureModel` declares only what is intrinsic to its own table: the composite PK and the unique index.

## DbContext Pooling

`AddLlamaShearsData` registers `LlamaShearsDbContext` via `AddDbContextPool`. Connection string is bound from the `Data` configuration section through `LlamaShearsDataOptions`; the `BindConfiguration` pattern keeps the registration extension free of `IConfiguration` parameters.

Interceptors are registered as singletons and attached to the pool via `options.AddInterceptors(...)` inside the pool factory.

## Migrations

EF logic — including migration history — lives in `LlamaShears.Data`. Migrations are generated and applied with `LlamaShears.Api.Host` as the startup project so the design-time DbContext is built through the host's full DI graph:

```
dotnet ef migrations add <Name> \
  --project src/LlamaShears.Data \
  --startup-project src/LlamaShears.Api.Host
```

`dotnet-ef` is installed as a local tool (`dotnet-tools.json` at the repo root), not globally.
