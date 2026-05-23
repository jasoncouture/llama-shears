using System.Reflection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Resources;

namespace LlamaShears;

public sealed class AppResourceDetector : IResourceDetector
{
    private readonly IHostEnvironment _environment;
    private readonly string _instanceId = Guid.CreateVersion7().ToString();

    public AppResourceDetector(IHostEnvironment environment)
    {
        _environment = environment;
    }

    public Resource Detect()
    {
        var attributes = new List<KeyValuePair<string, object>>
        {
            new KeyValuePair<string, object>("service.name", _environment.ApplicationName),
            new KeyValuePair<string, object>("service.namespace", "LlamaShears"),
            new KeyValuePair<string, object>("service.instance.id", _instanceId),
            new KeyValuePair<string, object>("deployment.environment.name", _environment.EnvironmentName),
        };

        var version = Assembly.GetEntryAssembly()?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;
        if (!string.IsNullOrWhiteSpace(version))
        {
            attributes.Add(new KeyValuePair<string, object>("service.version", version));
        }

        return new Resource(attributes);
    }
}
