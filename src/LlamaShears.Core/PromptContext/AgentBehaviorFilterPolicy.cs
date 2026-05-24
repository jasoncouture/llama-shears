using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Core.PromptContext;

internal static class AgentBehaviorFilterPolicy
{
    public static bool IsEnabled(FilterOptions? options)
        => options?.Default != FilterPolicy.Disable;

    public static bool Allowed(FilterOptions? options, string name)
    {
        if (options is null || options.Default == FilterPolicy.Default) return true;
        var contains = options.Names?.Contains(name) ?? false;
        return options.Default switch
        {
            FilterPolicy.Allow => contains,
            FilterPolicy.Deny => !contains,
            FilterPolicy.Disable => false,
            _ => true,
        };
    }
}
