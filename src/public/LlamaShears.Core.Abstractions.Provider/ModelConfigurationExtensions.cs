using LlamaShears.Core.Abstractions.Common;

namespace LlamaShears.Core.Abstractions.Provider;

public static class ModelConfigurationExtensions
{
    public static ModelConfiguration? GetModelConfiguration(this IDataContextScope scope)
    {
        scope.TryGetValue<ModelConfiguration>(ModelConfiguration.DataKey, out var config);
        return config;
    }
}
