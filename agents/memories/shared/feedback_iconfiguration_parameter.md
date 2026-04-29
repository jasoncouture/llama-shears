---
name: Never accept IConfiguration as a parameter
description: Use OptionsBuilder.BindConfiguration over IConfiguration parameters in DI extensions
type: feedback
---

Do not accept `IConfiguration` (or `IConfigurationSection`) as a parameter on DI extension methods like `AddXyzProvider`. Use `services.AddOptions<TOptions>().BindConfiguration("Section:Path")` instead — this resolves `IConfiguration` from the DI container at options-build time.

Allow the section name (string) to be passed as a parameter, defaulted, and placed last (least-likely to be set manually).

**Why:** Passing `IConfiguration` couples consumers to having a config object on hand at registration time and breaks composition with hosts that build configuration after `IServiceCollection` is set up. `BindConfiguration` defers resolution to DI and keeps the public API tight. Treat any new IConfiguration parameter as a code smell.

**How to apply:** When adding `AddXxx` extension methods that need configuration:
1. Don't take `IConfiguration` as a parameter — only the section name string (defaulted).
2. Use `AddOptions<TOptions>().BindConfiguration(sectionName)`.
3. Reference `Microsoft.Extensions.Options.ConfigurationExtensions` (provides `BindConfiguration`).

Only accept `IConfiguration` directly if **absolutely necessary** — e.g. when you must read values during registration (rare) and DI deferral is impossible.
