# LlamaShears.Plugins.IPlugin

Assembly: `LlamaShears.Plugins`

Contract every plugin exposes. Instances are produced by the
assembly's `IPluginLoader<IPlugin>` implementation and
threaded through the host's startup, request pipeline, and lifecycle.

## Properties

### `Name`

Stable identifier the host uses to disambiguate plugins.
Defaults to the implementing type's simple name; override only
when the host needs to distinguish two plugins that share one.

## Methods

### `Build`(IApplicationBuilder applicationBuilder)

Hook into the host's HTTP request pipeline (middleware,
endpoint routing, etc.). Invoked exactly once after
[IPlugin](IPlugin.md).`Register`. Default is a no-op so plugins that
don't expose HTTP surface can ignore it.

### `InitializeAsync`(IServiceProvider services, CancellationToken cancellationToken)

Asynchronous one-shot initialization, run after the service
provider has been built. Default is a completed value task.

### `Register`(IServiceCollection services)

Register the plugin's services with the host's DI container.
Invoked exactly once during host construction, before any
IServiceProvider is built.

### `UnloadingAsync`(IServiceProvider services, CancellationToken cancellationToken)

Called when the plugin is being unloaded — graceful cleanup of
state the plugin owns outside the DI container. Default is a
completed value task.

