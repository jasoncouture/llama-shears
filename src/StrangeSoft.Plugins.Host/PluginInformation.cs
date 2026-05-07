using System.Collections.Immutable;

namespace StrangeSoft.Plugins.Host;

public record PluginInformation(
    string Name,
    string Path,
    bool UseDefaultResolvers = true,
    ImmutableArray<IAssemblyResolver> AdditionalResolvers = default)
{
    public ImmutableArray<IAssemblyResolver> AdditionalResolvers { get; init; }
        = AdditionalResolvers.IsDefault ? [] : AdditionalResolvers;
}
