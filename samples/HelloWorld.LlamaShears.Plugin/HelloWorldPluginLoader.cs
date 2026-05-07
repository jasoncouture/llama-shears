using System.Collections.Immutable;
using LlamaShears.Plugins;
using StrangeSoft.Plugins.Abstractions;

namespace HelloWorld.LlamaShears.Plugin;

public sealed class HelloWorldPluginLoader : IPluginLoader<IPlugin>
{
    public Task<ImmutableArray<IPlugin>> LoadAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(ImmutableArray.Create<IPlugin>(new HelloWorldPlugin()));
    }
}
