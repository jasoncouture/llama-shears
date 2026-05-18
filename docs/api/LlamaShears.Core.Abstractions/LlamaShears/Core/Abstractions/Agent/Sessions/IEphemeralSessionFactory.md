# LlamaShears.Core.Abstractions.Agent.Sessions.IEphemeralSessionFactory

Assembly: `LlamaShears.Core.Abstractions`

Creates ephemeral sessions for a loaded agent. The new session gets a
fresh Guid id, its own service scope, its own data
context overlay (so prompt-template parameters and the parent
reference don't leak back to the main agent), and an empty transcript
— no parent turns are inherited.

## Methods

### `CreateAsync`([EphemeralSessionReference](EphemeralSessionReference.md) parent, [EphemeralSessionRequest](EphemeralSessionRequest.md) request, CancellationToken cancellationToken)

Creates and returns a session targeting `parent`
for its `session_reply` output. The session is not started
until the caller invokes [IEphemeralSession](IEphemeralSession.md).`RunAsync`;
the caller owns disposal.

