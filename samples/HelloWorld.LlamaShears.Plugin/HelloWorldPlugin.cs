using LlamaShears.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HelloWorld.LlamaShears.Plugin;

public sealed class HelloWorldPlugin : IPlugin
{
    public void Register(IServiceCollection services)
    {
    }

    public Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var logger = services.GetService<ILogger<HelloWorldPlugin>>();
        if (logger is not null)
        {
            logger.LogInformation("Hello, world! — from {Plugin}", nameof(HelloWorldPlugin));
        }
        else
        {
            Console.WriteLine("Hello, world! — from HelloWorldPlugin");
        }
        return Task.CompletedTask;
    }
}
