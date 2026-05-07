# Plugins

LlamaShears ships a plugin SDK so external code can extend the host without recompiling it. The chassis is split into four packages along two lines:

- **Generic vs. LlamaShears-specific.** The `StrangeSoft.Plugins.*` pair is a reusable plugin framework that doesn't know anything about LlamaShears — `T` is whatever plugin contract a host wants. The `LlamaShears.Plugins[.Host]` pair adds the LlamaShears-specific layer on top.
- **Plugin-author vs. host.** `*.Abstractions` packages are what plugin authors compile against. `*.Host` packages are what the host references; plugin authors do not.

The packages:

| Package | Audience | Role |
|---------|----------|------|
| [`StrangeSoft.Plugins.Abstractions`](../../src/public/StrangeSoft.Plugins.Abstractions/) | Plugin author | The chassis entry-point contract — `IPluginLoader<T>`. |
| [`StrangeSoft.Plugins.Host`](../../src/public/StrangeSoft.Plugins.Host/) | Host | The chassis itself — `PluginContext<T>`, `IPluginLocator<T>`, `PluginInformation`, the `Plugin` orchestrator, the resolver chain, the logger interface. |
| [`LlamaShears.Plugins`](../../src/public/LlamaShears.Plugins/) | Plugin author | The LlamaShears `IPlugin` contract a plugin implements. |
| [`LlamaShears.Plugins.Host`](../../src/public/LlamaShears.Plugins.Host/) | Host | DI plumbing on top of the chassis — `LoadPluginsAsync`, `UsePluginsAsync`, the snapshot, the deferred logger. |

A reference plugin lives at [`samples/HelloWorld.LlamaShears.Plugin`](../../samples/HelloWorld.LlamaShears.Plugin/) — the minimum viable shape (one `IPlugin` plus one `IPluginLoader<IPlugin>`).

## What a plugin looks like

A plugin assembly exposes two things:

```csharp
public sealed class HelloWorldPlugin : IPlugin
{
    public void Register(IServiceCollection services)
    {
        // Add services. Runs inside a transactional snapshot — if this
        // throws, the entire IPlugin's registrations are rolled back.
    }

    public Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var logger = services.GetService<ILogger<HelloWorldPlugin>>();
        logger?.LogInformation("Hello, world! — from {Plugin}", nameof(HelloWorldPlugin));
        return Task.CompletedTask;
    }
}

public sealed class HelloWorldPluginLoader : IPluginLoader<IPlugin>
{
    public Task<ImmutableArray<IPlugin>> LoadAsync(CancellationToken cancellationToken)
        => Task.FromResult(ImmutableArray.Create<IPlugin>(new HelloWorldPlugin()));
}
```

`IPlugin` is the LlamaShears contract; `IPluginLoader<IPlugin>` is the StrangeSoft chassis entry point. The host's loader chassis discovers `IPluginLoader<T>` implementations in the plugin assembly via reflection, instantiates each via parameterless constructor, and calls `LoadAsync` to materialize the actual `IPlugin` instances. An empty array is the canonical "this assembly chose not to activate" signal — the host treats it as a successful no-op rather than an error.

## `IPlugin` lifecycle

```
plugin loader returns IPlugin
       │
       ▼
IServiceCollection snapshot { ↺ rollback on throw }
       │
       ▼
plugin.Register(services)            ← still pre-build
       │
       ▼
services.AddSingleton<IPlugin>(plugin)
       │
       ▼
{ DI build }
       │
       ▼
foreach plugin in app.Services.GetServices<IPlugin>():
    plugin.InitializeAsync(services, cancellationToken)   ← async, await each
       │
       ▼
foreach plugin in app.Services.GetServices<IPlugin>():
    plugin.Build(applicationBuilder)                      ← sync, pipeline wiring

         (host runs)

       ▼
plugin.UnloadingAsync(cancellationToken)                  ← on host shutdown
```

Each method has a default implementation:

| Method | Default | When to override |
|--------|---------|------------------|
| `Register(IServiceCollection)` | required | Always — at minimum, the place to register your plugin's own services. |
| `Build(IApplicationBuilder)` | no-op | Only when you need to insert middleware or map endpoints. |
| `InitializeAsync(IServiceProvider, CancellationToken)` | `Task.CompletedTask` | Async startup — open files, warm caches, subscribe to events. |
| `UnloadingAsync(CancellationToken)` | `Task.CompletedTask` | Async teardown — flush writes, close connections. |

`Register` runs inside a transactional `IServiceCollectionSnapshot`. If it throws, every registration the plugin made (including the `AddSingleton<IPlugin>(plugin)` call) is rolled back, so a partially-applied plugin never ships into the running container. The host sees the failure via the `failureCallback` passed to `LoadPluginsAsync`; returning `true` from that callback swallows the error and the loader moves on, returning `false` (or `null`) rethrows. The default in `Program.cs` is rethrow — a broken plugin is a fatal startup error.

## Type unification across the load context

Each plugin loads into its own `AssemblyLoadContext`. That gives plugins isolation but creates a problem: if a plugin's `IPlugin` is loaded into the plugin's ALC and the host's `IPlugin` lives in the Default ALC, the runtime sees them as two distinct `Type`s and DI registration fails (`AddSingleton<IPlugin>(plugin)` doesn't match the host's `IServiceProvider.GetServices<IPlugin>()`).

`HostContextAssemblyResolver` solves this by short-circuiting host-owned assembly names to the Default ALC's copy:

1. At host startup, `HostContextAssemblyResolver.Initialize(hostAssembly)` walks the host's transitive reference graph (`Assembly.GetReferencedAssemblies()` recursively, eagerly loading each into Default) and records the set of host-owned names.
2. Each plugin's `AssemblyLoadContext` has the resolver chained on its `Resolving` event. When the runtime asks the plugin's ALC for an assembly, the resolver checks "is this a host-owned name?" — if yes, it returns `AssemblyLoadContext.Default.LoadFromAssemblyName(assemblyName)` so the plugin gets the host's already-loaded copy. If no, the next resolver in the chain handles it (typically `PathAssemblyResolver`, which uses the plugin's `deps.json`).

The result: every host-shared type (`IPlugin`, `ILogger<>`, framework primitives) has stable identity across the host and every plugin. Provider-side state and consumer-side state line up.

## Discovery surface

Plugins don't have to live in any particular location. The host decides where to find them via `IPluginLocator<T>`:

```csharp
public interface IPluginLocator<T> where T : class
{
    IEnumerable<PluginInformation> GetPluginInformation();
}

public record PluginInformation(
    string Name,
    string Path,
    bool UseDefaultResolvers = true,
    ImmutableArray<IAssemblyResolver> AdditionalResolvers = default);
```

Each `PluginInformation` describes one plugin: its display name, the absolute path to its root assembly, whether to wire the canonical resolver chain (`HostContextAssemblyResolver` + `PathAssemblyResolver`), and any extra resolvers to chain on top.

The shipping host uses [`PathPluginLoader`](../../src/LlamaShears/PluginLoaders/PathPluginLoader.cs) — a path-list locator that probes each path, drops the ones that don't exist, and yields `PluginInformation` for the rest. Followups in [TASKS.md](../../TASKS.md) cover NuGet-package and one-config-field-with-auto-detect locators.

## Orchestration

`Plugin.LoadPluginContexts<T>(...)` is the static orchestrator that drives the chassis end-to-end. Given a list of `IPluginLocator<T>`, it:

1. Calls `HostContextAssemblyResolver.TryInitialize(hostAssembly)` so the host-owned set is built (idempotent — second call is a no-op).
2. For each locator, asks for `GetPluginInformation()` and yields one `IPluginContext<T>` per record.
3. Each `IPluginContext<T>` stands up a fresh `AssemblyLoadContext` for that plugin, loads its root assembly into it, and chains the canonical resolver chain (or a custom one).
4. `IPluginContext<T>.LoadPluginsAsync` then walks the loaded assemblies, finds every `IPluginLoader<T>` implementation, instantiates it via parameterless constructor, and calls `LoadAsync`. Loader exceptions are swallowed and logged via `IPluginContextLogger`.

The orchestrator is intentionally lazy. A locator that yields ten `PluginInformation` records produces ten `IPluginContext<T>` instances on demand, and each one's plugins are streamed as they're loaded — a slow plugin doesn't hold up the discovery of the next one.

## DI integration

`LoadPluginsAsync` (in `LlamaShears.Plugins.Host`) is what the host's composition root calls:

```csharp
await builder.Services.LoadPluginsAsync(
    failureCallback: null,
    cancellationToken: CancellationToken.None,
    new PathPluginLoader(pluginPaths));

var app = builder.Build();
await app.UsePluginsAsync(app.Lifetime.ApplicationStopping);
```

What `LoadPluginsAsync` does:

1. Wires the **deferred logger** (`AddPluginDefferedLogger`) — see below.
2. Iterates `Plugin.LoadPluginContexts(pluginLocators)` lazily, awaiting each `IPluginContext<IPlugin>`.
3. For each plugin yielded by the context, calls `TryApplyPlugin` — which opens an `IServiceCollectionSnapshot`, calls `IPlugin.Register(services)`, registers the plugin as `AddSingleton<IPlugin>(plugin)`, and accepts the snapshot. On exception, the snapshot rolls back; the `failureCallback` decides whether to swallow or rethrow.

`UsePluginsAsync(IApplicationBuilder)` runs after `app.Build()`. It pulls `app.Services.GetServices<IPlugin>()` and walks them through `InitializeAsync` (await each) and then `Build(IApplicationBuilder)` (synchronous). Two passes rather than one because `Build` may need to insert middleware after every plugin has had a chance to register services.

## Deferred logger

The plugin chassis runs **before** the DI container is built — registration happens against an `IServiceCollection`, not an `IServiceProvider`. The chassis's own log calls (during `HostContextAssemblyResolver.Initialize`, during loader discovery, during `IPlugin.Register`) therefore can't go through `ILogger<T>` — there is no logger factory yet.

`DeferredPluginHostLogger` solves this by buffering. Every chassis log call lands in an `IPluginContextLogger` (the `Debug` / `Information` / `Warning` / `Error` ILogger-style interface), and the deferred logger captures each call as a `DeferredLogEntry` in a monitor-locked list — preserving arrival order across concurrent loaders.

Once DI is built, `LoggingStartupHostedService` (an `IHostedService` registered by `AddPluginDefferedLogger`) calls `RedirectTo(ILogger)` on the deferred logger. The buffer drains onto the real `ILoggerFactory`, and from that point forward the deferred logger writes inline rather than buffering.

The handoff is one-shot: there's exactly one drain, after which the chassis is reading the real logger directly. A late log call that races with the drain still lands correctly because `RedirectTo` takes the gate before flipping the underlying writer.

## What's still followup

Tracked as TASKS.md entries (and corresponding GitHub issues):

- **NuGet-package plugin loading** — download + load from nuget.org-shaped feeds.
- **Plugin source flexibility** — one config field, auto-detected: `Package.Name@SemVer`, `path/to/Assembly.dll`, `some.package.nupkg`.
- **Skills support** — bundled prompts / tool-sets / channel wiring a plugin can ship.
- **Sub-agents (depth + per-tree budget).** A plugin that wants to spawn an agent for a sub-task will need this surface to land first.

The chassis itself — `IPlugin`, `IPluginLoader<T>`, `IPluginContext<T>`, the resolver chain, the snapshot, the deferred logger, the orchestrator — is shipped and exercised by the HelloWorld sample.
