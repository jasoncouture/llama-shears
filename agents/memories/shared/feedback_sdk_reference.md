---
name: "SDK reference" means FrameworkReference, not the Sdk attribute
description: Don't change the project's Sdk attribute when asked to add an SDK reference
type: feedback
---

When the user says "add an SDK reference" (e.g. "the .Api project will need an SDK reference"), they mean adding a `<FrameworkReference Include="..." />` element (typically `Microsoft.AspNetCore.App`) to an existing `Microsoft.NET.Sdk` project.

Do **not** change the `Sdk` attribute on `<Project>` (e.g. swapping `Microsoft.NET.Sdk` to `Microsoft.NET.Sdk.Web`).

**Why:** Swapping the SDK changes a lot of build behavior (web publishing, default item globs, etc.) that the user did not ask for. A `FrameworkReference` is the minimal, targeted change — it just makes a runtime/framework available to a class library. The user pushed back hard on this exact misread.

**How to apply:** When asked to add an SDK reference, edit `<ItemGroup>` to add `<FrameworkReference Include="Microsoft.AspNetCore.App" />` (or whatever framework the user named) and leave the `Sdk` attribute alone.
