using System.Collections.Immutable;
using LlamaShears.Core.Abstractions.Agent.Sessions;

namespace LlamaShears.Core.Abstractions.Commands;

/// <summary>
/// Per-invocation input for an <see cref="ISlashCommand"/>. Carries the
/// session the command is acting on plus the positional arguments parsed
/// from the input line.
/// </summary>
/// <param name="Session">The session in scope for this command invocation.</param>
/// <param name="Arguments">Positional arguments after the command name, in input order. Empty when none were supplied.</param>
public sealed record SlashCommandContext(SessionId Session, ImmutableArray<string> Arguments);
