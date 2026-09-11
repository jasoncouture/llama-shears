namespace LlamaShears.Core.Abstractions.Agent;

/// <summary>
/// Inputs for <see cref="ISubagentRunner.RunAsync"/>.
/// </summary>
/// <param name="Prompt">User-role instruction for the child. Required.</param>
/// <param name="Model">
/// Optional <c>provider/model</c> overlay for the child. Unparseable
/// values are refused. Not a free-form URL.
/// </param>
/// <param name="Context">Optional preamble prepended to <paramref name="Prompt"/>.</param>
/// <param name="MaxTurns">
/// Optional child <see cref="AgentToolConfig.TurnLimit"/> (1–64).
/// Omitted keeps the parent's tool budget.
/// </param>
/// <param name="AwaitResult">
/// When <see langword="true"/> (default), wait for the child to
/// finish and return its last assistant text. When
/// <see langword="false"/>, return after spawn.
/// </param>
/// <param name="TimeoutSeconds">
/// Await ceiling in seconds (1–600). Default 120. Ignored when
/// <paramref name="AwaitResult"/> is <see langword="false"/>.
/// </param>
public sealed record SubagentRunRequest(
    string Prompt,
    string? Model = null,
    string? Context = null,
    int? MaxTurns = null,
    bool AwaitResult = true,
    int TimeoutSeconds = SubagentRunRequest.DefaultTimeoutSeconds)
{
    /// <summary>Default await ceiling when the caller omits a timeout.</summary>
    public const int DefaultTimeoutSeconds = 120;

    /// <summary>Smallest accepted <see cref="TimeoutSeconds"/>.</summary>
    public const int MinTimeoutSeconds = 1;

    /// <summary>Largest accepted <see cref="TimeoutSeconds"/>.</summary>
    public const int MaxTimeoutSeconds = 600;

    /// <summary>Smallest accepted <see cref="MaxTurns"/>.</summary>
    public const int MinMaxTurns = 1;

    /// <summary>Largest accepted <see cref="MaxTurns"/>.</summary>
    public const int MaxMaxTurns = 64;
}
