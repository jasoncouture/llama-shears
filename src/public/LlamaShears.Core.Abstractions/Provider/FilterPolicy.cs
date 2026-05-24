namespace LlamaShears.Core.Abstractions.Provider;

/// <summary>
/// Selects how a <see cref="FilterOptions"/> instance interprets its name list.
/// </summary>
public enum FilterPolicy
{
    /// <summary>No filtering — every name is allowed and the name list is ignored.</summary>
    Default,

    /// <summary>The name list is an allowlist — only names it contains are allowed.</summary>
    Allow,

    /// <summary>The name list is a denylist — every name except those it contains is allowed.</summary>
    Deny,

    /// <summary>The entire subsystem gated by this filter is turned off; nothing is allowed regardless of the name list.</summary>
    Disable,
}
