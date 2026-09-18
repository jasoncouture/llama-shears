---
name: Chat UI data-context drawer
description: Header toggle plus a snapshot drawer for the selected session's IDataContextScope
type: project
---

Chat header toggle `ShowDataContext` (default off, localStorage `llamashears:agent:{id}:showDataContext`), braces icon. Opening it renders `DataContextPanel` beside the conversation.

The panel does not read `IDataContextFactory.Current` blindly. `IDataContextInspector.Inspect` does `TryJoinContextScope` + `Snapshot()`, then `ClearCurrent(false)` unless that scope was already Current on the Blazor circuit (a different Current throws and is shown as an error). Values are pretty-printed JSON via `DataContextValueFormatter`; unserializable values fall back to `ToString()`.

This is the operator glance surface, not the TASKS.md "Data explorer" page.
