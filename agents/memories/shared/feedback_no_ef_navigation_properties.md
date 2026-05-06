---
name: No EF navigation properties; relationships declared on principal side
description: Entities expose scalars + FK columns only; relationship config lives on the referenced (principal) entity, not the referencing one
type: feedback
---

EF entities in this project must not declare navigation properties — neither reference navs (e.g. `Session Session { get; set; }`) nor collection navs (e.g. `ICollection<SessionMessage> Messages { get; set; }`). Only scalar properties and raw foreign-key columns belong on the entity.

Related lookups are performed explicitly through DTO mappers / query layers using the FK column.

Relationship configuration (HasMany / HasOne / HasForeignKey / OnDelete) lives in the **principal** (referenced) entity's `ConfigureModel`, not the dependent (referencing) entity's. The dependent side already advertises the relationship via its FK column; the principal needs the config to give visibility into "who points at me." Each entity's `ConfigureModel` therefore acts as a one-stop view of inbound relationships.

**Why:** Navigation properties make it trivially easy to silently introduce eager loading, lazy loading, or N+1 patterns whose cost is invisible at the call site. The user has been bitten by this and prefers the cost to be visible in code review. Putting relationship config on the principal recovers the "who references me" visibility navs would have given, without the runtime hazards.

**How to apply:**
- Adding a new entity: declare only scalars + FK `Guid`/`int` columns. No `Foo Foo { get; set; }`, no `ICollection<Bar> Bars { get; set; }`.
- Configure the relationship in the principal's `ConfigureModel`:
  `entity.HasMany<TDependent>().WithOne().HasForeignKey(d => d.PrincipalId).OnDelete(...)`.
- 1:1 follows the same rule: principal calls `HasOne<TDependent>().WithOne().HasForeignKey<TDependent>(d => d.PrincipalId)`.
- Indexes belong on the entity whose table they live on (typically the dependent), even when the index supports lookups initiated from the principal — they're a storage-shape concern, not a relationship concern.
- When a related entity is needed at runtime, write an explicit query (or DTO mapper). Do not "fix" missing-navigation compile errors by adding the navigation back.
