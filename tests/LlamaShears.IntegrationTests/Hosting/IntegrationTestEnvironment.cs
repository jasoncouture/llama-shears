using System.Runtime.CompilerServices;

namespace LlamaShears.IntegrationTests.Hosting;

internal static class IntegrationTestEnvironment
{
    [ModuleInitializer]
    public static void Initialize()
    {
        Environment.SetEnvironmentVariable("LLAMASHEARS_NO_LOCAL_CONFIG", "true");
    }
}
