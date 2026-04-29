---
name: Storage moves from EF/DB to chunked JSON on disk
description: Planned removal of Entity Framework and the database in favor of atomic, chunked JSON files on disk; current vs. archived conversation lives in folder placement
type: project
---

EF and the database are being removed. Persistence will be chunked JSON files on disk, written atomically.

**Why:**
- Disk-based JSON simplifies the model: no schema, no migrations, no EF.
- Atomic writes are straightforward at the file level.
- Conversation history management collapses to filesystem moves: the currently active conversation lives in a designated "current" directory; archiving a conversation is a `mv` to another folder. No DB rows to mutate, no soft-delete columns, no joins.

**How to apply:**
- Treat EF/DB as scheduled-for-removal. Don't add new entities, hooks, or migrations beyond what's needed to keep the build green until the swap.
- New persistence work should target the disk/JSON model, not the EF model.
- The "current conversation lives in *that* directory" rule means folder placement is the source of truth for which conversation is active — design APIs around that, not around an `IsActive` flag.
- This is documented intent, not yet implemented. Confirm scope before starting the actual EF removal.
